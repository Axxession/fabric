using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Fabric.Hardware.Agent.Devices;
using Fabric.Hardware.Agent.Gateway;
using Fabric.Hardware.Agent.Options;
using Fabric.Hardware.Contracts;
using Fabric.Hardware.Contracts.Capabilities;
using Fabric.Hardware.Contracts.Commands;
using Fabric.Hardware.Contracts.Rfid;
using Microsoft.Extensions.Options;

namespace Fabric.Hardware.Agent;

public sealed class CommandWorker(
    HardwareGatewayClient gatewayClient,
    IReadOnlyList<IQrReaderDevice> qrReaders,
    IReadOnlyList<IDispenserDevice> dispensers,
    IReadOnlyList<ICollectorDevice> collectors,
    IReadOnlyList<IEncoderDevice> encoders,
    IReadOnlyList<IRfidReaderDevice> rfidReaders,
    TimeProvider timeProvider,
    IOptions<HardwareAgentOptions> options,
    ILogger<CommandWorker> logger) : BackgroundService
{
    private readonly SemaphoreSlim _drainLock = new(1, 1);
    private readonly ConcurrentDictionary<Guid, ActiveCommandExecution> _activeCommands = [];
    private int _drainRequested;
    private int _drainWorkerActive;
    private readonly IReadOnlyDictionary<string, ICollectorDevice> _collectors = collectors.ToDictionary(collector => collector.DeviceId, StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyDictionary<string, IDispenserDevice> _dispensers = dispensers.ToDictionary(dispenser => dispenser.DeviceId, StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyDictionary<string, IEncoderDevice> _encoders = encoders.ToDictionary(encoder => encoder.DeviceId, StringComparer.OrdinalIgnoreCase);
    private readonly HardwareAgentOptions _options = options.Value;
    private readonly IReadOnlyDictionary<string, IQrReaderDevice> _qrReaders = qrReaders.ToDictionary(qrReader => qrReader.DeviceId, StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyDictionary<string, IRfidReaderDevice> _rfidReaders = rfidReaders.ToDictionary(rfidReader => rfidReader.DeviceId, StringComparer.OrdinalIgnoreCase);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.WorkerStarted(nameof(CommandWorker));

        Task streamTask = RunCommandStreamAsync(stoppingToken);
        Task reconcileTask = RunReconciliationAsync(stoppingToken);

        try
        {
            await Task.WhenAll(streamTask, reconcileTask);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        logger.WorkerStopped(nameof(CommandWorker));
    }

    private async Task RunCommandStreamAsync(CancellationToken stoppingToken)
    {
        int failures = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.CommandStreamConnected();
                failures = 0;
                await ReconcileActiveCommandStatusesAsync(stoppingToken);

                await foreach (HardwareCommandStreamEvent commandEvent in gatewayClient.StreamCommandEventsAsync(stoppingToken))
                {
                    if (commandEvent.Type == HardwareCommandEventType.CommandCancelled)
                    {
                        HandleCommandCancelled(commandEvent);
                        continue;
                    }

                    logger.CommandNotificationReceived(commandEvent.CommandId);
                    RequestDrain(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                failures++;
                if (_activeCommands.IsEmpty)
                    logger.CommandStreamDisconnected(ex);
                else
                    logger.CommandStreamDisconnectedWithActiveCommands(_activeCommands.Count, ex);

                await PollUntilReconnectAsync(GetReconnectDelay(failures), stoppingToken);
            }
        }
    }

    private async Task RunReconciliationAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_options.CommandReconcileInterval, stoppingToken);
            RequestDrain(stoppingToken);
        }
    }

    private async Task PollUntilReconnectAsync(TimeSpan reconnectDelay, CancellationToken stoppingToken)
    {
        using var reconnectTimeout = new CancellationTokenSource(reconnectDelay);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, reconnectTimeout.Token);

        while (!linkedCancellation.IsCancellationRequested)
        {
            try
            {
                RequestDrain(stoppingToken);
                await ReconcileActiveCommandStatusesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.CommandPollFailed(ex);
            }

            try
            {
                TimeSpan nextDelay = _activeCommands.IsEmpty ? _options.CommandPollInterval : _options.CommandReconcileInterval;
                await Task.Delay(nextDelay, linkedCancellation.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task DrainCommandsAsync(CancellationToken stoppingToken)
    {
        await _drainLock.WaitAsync(stoppingToken);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                HardwareCommandEnvelope? command = await gatewayClient.GetNextCommandAsync(stoppingToken);
                if (command is null)
                    return;

                logger.CommandFetched(command.CommandId, command.DeviceId, command.Capability);

                await ProcessCommandAsync(command, stoppingToken);
            }
        }
        finally
        {
            _drainLock.Release();
        }
    }

    private void RequestDrain(CancellationToken stoppingToken)
    {
        Interlocked.Exchange(ref _drainRequested, 1);
        if (Interlocked.CompareExchange(ref _drainWorkerActive, 1, 0) != 0)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (Interlocked.Exchange(ref _drainRequested, 0) == 0)
                        break;

                    try
                    {
                        await DrainCommandsAsync(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.CommandPollFailed(ex);
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _drainWorkerActive, 0);
                if (!stoppingToken.IsCancellationRequested && Interlocked.CompareExchange(ref _drainRequested, 0, 0) == 1)
                    RequestDrain(stoppingToken);
            }
        }, CancellationToken.None);
    }

    private async Task ProcessCommandAsync(HardwareCommandEnvelope command, CancellationToken stoppingToken)
    {
        HardwareCommandClaimResponse? claim = await gatewayClient.ClaimCommandAsync(command.CommandId, stoppingToken);
        if (claim is null)
        {
            logger.CommandClaimLost(command.CommandId);
            return;
        }

        logger.CommandClaimed(claim.Command.CommandId, claim.Command.DeviceId, claim.Command.Capability);
        logger.CommandExecuting(claim.Command.CommandId, claim.Command.DeviceId, claim.Command.Capability);
        using ActiveCommandExecution execution = new(claim.Command.CommandId, claim.Command.DeviceId, claim.Command.Capability);
        _activeCommands[claim.Command.CommandId] = execution;

        try
        {
            PostHardwareCommandResultRequest result = await ExecuteCommandAsync(claim.Command, stoppingToken, execution);
            await gatewayClient.PostCommandResultAsync(command.CommandId, result, stoppingToken);
            logger.CommandCompleted(claim.Command.CommandId, claim.Command.DeviceId, claim.Command.Capability, result.Status);
        }
        finally
        {
            _activeCommands.TryRemove(claim.Command.CommandId, out _);
        }
    }

    private async Task<PostHardwareCommandResultRequest> ExecuteCommandAsync(HardwareCommandEnvelope command, CancellationToken stoppingToken, ActiveCommandExecution execution)
    {
        if (_qrReaders.TryGetValue(command.DeviceId, out IQrReaderDevice? qrReader))
            return await ExecuteQrCommandAsync(command, qrReader, stoppingToken, execution);

        if (_dispensers.TryGetValue(command.DeviceId, out IDispenserDevice? dispenser))
            return await ExecuteDispenserCommandAsync(command, dispenser, stoppingToken, execution);

        if (_collectors.TryGetValue(command.DeviceId, out ICollectorDevice? collector))
            return await ExecuteCollectorCommandAsync(command, collector, stoppingToken, execution);

        if (_encoders.TryGetValue(command.DeviceId, out IEncoderDevice? encoder))
            return await ExecuteEncoderCommandAsync(command, encoder, stoppingToken, execution);

        if (_rfidReaders.TryGetValue(command.DeviceId, out IRfidReaderDevice? rfidReader))
            return await ExecuteRfidCommandAsync(command, rfidReader, stoppingToken, execution);

        return Failure(HardwareOperationStatus.DeviceUnavailable, "device_unavailable", "Device is not available on this agent.");
    }

    private async Task<PostHardwareCommandResultRequest> ExecuteRfidCommandAsync(HardwareCommandEnvelope command, IRfidReaderDevice rfidReader, CancellationToken stoppingToken, ActiveCommandExecution execution)
    {
        if (!string.Equals(command.Capability, HardwareCapabilities.RfidRead, StringComparison.OrdinalIgnoreCase))
            return Failure(HardwareOperationStatus.Failed, "unsupported_capability", "Capability is not supported by this device.");

        using var timeout = new CancellationTokenSource(_options.CommandTimeout);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token, execution.RemoteCancellation.Token);

        try
        {
            execution.SetPhase("rfid read");
            string cardNumber = await rfidReader.ReadCardAsync(linkedCancellation.Token);
            var result = new JsonObject { ["cardNumber"] = cardNumber };
            return new PostHardwareCommandResultRequest(HardwareOperationStatus.Succeeded, result, null, timeProvider.GetUtcNow());
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
        {
            return Failure(HardwareOperationStatus.Timeout, "timeout", "RFID read timed out.");
        }
        catch (OperationCanceledException) when (execution.IsRemoteCancellationRequested)
        {
            return HandleRemoteCancellation(command, execution);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return Failure(HardwareOperationStatus.Cancelled, "cancelled", "RFID read was cancelled.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.CommandDeviceUnavailable(command.CommandId, rfidReader.DeviceId, ex);
            return Failure(HardwareOperationStatus.DeviceUnavailable, "device_unavailable", "RFID reader is not available.");
        }
    }

    private async Task<PostHardwareCommandResultRequest> ExecuteEncoderCommandAsync(HardwareCommandEnvelope command, IEncoderDevice encoder, CancellationToken stoppingToken, ActiveCommandExecution execution)
    {
        using var timeout = new CancellationTokenSource(_options.CommandTimeout);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token, execution.RemoteCancellation.Token);

        try
        {
            if (string.Equals(command.Capability, HardwareCapabilities.CardPresent, StringComparison.OrdinalIgnoreCase))
            {
                execution.SetPhase("waiting for card present");
                await encoder.WaitForCardPresentAsync(linkedCancellation.Token);
                var result = new JsonObject { ["present"] = true };
                return new PostHardwareCommandResultRequest(HardwareOperationStatus.Succeeded, result, null, timeProvider.GetUtcNow());
            }

            if (string.Equals(command.Capability, HardwareCapabilities.RfidApduExchange, StringComparison.OrdinalIgnoreCase))
            {
                execution.SetPhase("apdu exchange");
                return await ExchangeEncoderApduAsync(command.Payload, encoder, linkedCancellation.Token);
            }

            if (string.Equals(command.Capability, HardwareCapabilities.CardPrint, StringComparison.OrdinalIgnoreCase))
            {
                execution.SetPhase("card print");
                return await PrintEncoderCardAsync(command.Payload, encoder, linkedCancellation.Token);
            }

            if (string.Equals(command.Capability, HardwareCapabilities.CardEject, StringComparison.OrdinalIgnoreCase))
            {
                execution.SetPhase("waiting for card removal");
                await encoder.WaitForCardRemovalAsync(linkedCancellation.Token);
                var result = new JsonObject { ["removed"] = true };
                return new PostHardwareCommandResultRequest(HardwareOperationStatus.Succeeded, result, null, timeProvider.GetUtcNow());
            }

            return Failure(HardwareOperationStatus.Failed, "unsupported_capability", "Capability is not supported by this device.");
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
        {
            return Failure(HardwareOperationStatus.Timeout, "timeout", "Encoder command timed out.");
        }
        catch (OperationCanceledException) when (execution.IsRemoteCancellationRequested)
        {
            return HandleRemoteCancellation(command, execution);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return Failure(HardwareOperationStatus.Cancelled, "cancelled", "Encoder command was cancelled.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.CommandDeviceUnavailable(command.CommandId, encoder.DeviceId, ex);
            return Failure(HardwareOperationStatus.DeviceUnavailable, "device_unavailable", ex.Message);
        }
    }

    private async Task<PostHardwareCommandResultRequest> ExchangeEncoderApduAsync(JsonObject? payload, IEncoderDevice encoder, CancellationToken cancellationToken)
    {
        if (!TryReadApduRequest(payload, out RfidApduExchangeRequest? request, out PostHardwareCommandResultRequest? invalidPayload))
            return invalidPayload!;

        byte[] response = await encoder.ExchangeApduAsync(Convert.FromHexString(request!.CommandHex), cancellationToken);
        var result = new JsonObject { ["responseHex"] = Convert.ToHexString(response) };
        return new PostHardwareCommandResultRequest(HardwareOperationStatus.Succeeded, result, null, timeProvider.GetUtcNow());
    }

    private async Task<PostHardwareCommandResultRequest> PrintEncoderCardAsync(JsonObject? payload, IEncoderDevice encoder, CancellationToken cancellationToken)
    {
        if (!TryReadCardPrintRequest(payload, out string? frontImageBase64, out PostHardwareCommandResultRequest? invalidPayload))
            return invalidPayload!;

        await encoder.PrintAsync(Convert.FromBase64String(frontImageBase64!), cancellationToken);
        var result = new JsonObject { ["printed"] = true };
        return new PostHardwareCommandResultRequest(HardwareOperationStatus.Succeeded, result, null, timeProvider.GetUtcNow());
    }

    private async Task<PostHardwareCommandResultRequest> ExecuteQrCommandAsync(HardwareCommandEnvelope command, IQrReaderDevice qrReader, CancellationToken stoppingToken, ActiveCommandExecution execution)
    {
        if (!string.Equals(command.Capability, HardwareCapabilities.QrScan, StringComparison.OrdinalIgnoreCase))
            return Failure(HardwareOperationStatus.Failed, "unsupported_capability", "Capability is not supported by this device.");

        using var timeout = new CancellationTokenSource(_options.CommandTimeout);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token, execution.RemoteCancellation.Token);

        try
        {
            execution.SetPhase("qr scan");
            string value = await qrReader.ReadQrCodeAsync(linkedCancellation.Token);
            var result = new JsonObject { ["value"] = value };
            return new PostHardwareCommandResultRequest(HardwareOperationStatus.Succeeded, result, null, timeProvider.GetUtcNow());
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
        {
            return Failure(HardwareOperationStatus.Timeout, "timeout", "QR scan timed out.");
        }
        catch (OperationCanceledException) when (execution.IsRemoteCancellationRequested)
        {
            return HandleRemoteCancellation(command, execution);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return Failure(HardwareOperationStatus.Cancelled, "cancelled", "QR scan was cancelled.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.CommandDeviceUnavailable(command.CommandId, qrReader.DeviceId, ex);
            return Failure(HardwareOperationStatus.DeviceUnavailable, "device_unavailable", "QR reader is not available.");
        }
    }

    private async Task<PostHardwareCommandResultRequest> ExecuteDispenserCommandAsync(HardwareCommandEnvelope command, IDispenserDevice dispenser, CancellationToken stoppingToken, ActiveCommandExecution execution)
    {
        if (string.Equals(command.Capability, HardwareCapabilities.CardDrop, StringComparison.OrdinalIgnoreCase))
            return await DropCardAsync(command, dispenser, stoppingToken);

        if (string.Equals(command.Capability, HardwareCapabilities.CardPresent, StringComparison.OrdinalIgnoreCase))
            return await PresentCardAsync(command, dispenser, dropAfterRead: false, stoppingToken, execution);

        if (string.Equals(command.Capability, HardwareCapabilities.CardDispense, StringComparison.OrdinalIgnoreCase))
            return await PresentCardAsync(command, dispenser, dropAfterRead: true, stoppingToken, execution);

        return Failure(HardwareOperationStatus.Failed, "unsupported_capability", "Capability is not supported by this device.");
    }

    private async Task<PostHardwareCommandResultRequest> ExecuteCollectorCommandAsync(HardwareCommandEnvelope command, ICollectorDevice collector, CancellationToken stoppingToken, ActiveCommandExecution execution)
    {
        if (string.Equals(command.Capability, HardwareCapabilities.CardPresent, StringComparison.OrdinalIgnoreCase))
            return await PresentCollectorCardAsync(command, collector, stoppingToken, execution);

        if (string.Equals(command.Capability, HardwareCapabilities.CardCollect, StringComparison.OrdinalIgnoreCase))
            return await CollectCardAsync(command, collector, stoppingToken);

        if (string.Equals(command.Capability, HardwareCapabilities.CardEject, StringComparison.OrdinalIgnoreCase))
            return await EjectCardAsync(command, collector, stoppingToken, execution);

        return Failure(HardwareOperationStatus.Failed, "unsupported_capability", "Capability is not supported by this device.");
    }

    private async Task<PostHardwareCommandResultRequest> DropCardAsync(HardwareCommandEnvelope command, IDispenserDevice dispenser, CancellationToken stoppingToken)
    {
        try
        {
            bool dropped = await dispenser.DropAsync(stoppingToken);
            if (!dropped)
            {
                logger.DispenserCommandFailed(dispenser.DeviceId, command.Capability);
                return Failure(HardwareOperationStatus.Failed, "drop_failed", "Dispenser did not confirm card drop.");
            }

            var result = new JsonObject { ["dropped"] = true };
            return new PostHardwareCommandResultRequest(HardwareOperationStatus.Succeeded, result, null, timeProvider.GetUtcNow());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.CommandDeviceUnavailable(command.CommandId, dispenser.DeviceId, ex);
            return Failure(HardwareOperationStatus.DeviceUnavailable, "device_unavailable", "Dispenser is not available.");
        }
    }

    private async Task<PostHardwareCommandResultRequest> PresentCollectorCardAsync(HardwareCommandEnvelope command, ICollectorDevice collector, CancellationToken stoppingToken, ActiveCommandExecution execution)
    {
        if (!_rfidReaders.TryGetValue(collector.RfidReaderDeviceId, out IRfidReaderDevice? rfidReader))
            return Failure(HardwareOperationStatus.DeviceUnavailable, "rfid_reader_unavailable", "Configured RFID reader is not available on this agent.");

        try
        {
            execution.SetPhase("waiting for collector card present");
            using var waitCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, execution.RemoteCancellation.Token);
            await collector.WaitForCardAtReaderAsync(waitCancellation.Token);

            using var timeout = new CancellationTokenSource(collector.RfidReadTimeout);
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token, execution.RemoteCancellation.Token);

            try
            {
                execution.SetPhase("collector rfid read");
                string cardNumber = await rfidReader.ReadCardAsync(linkedCancellation.Token);
                var result = new JsonObject { ["cardNumber"] = cardNumber };
                return new PostHardwareCommandResultRequest(HardwareOperationStatus.Succeeded, result, null, timeProvider.GetUtcNow());
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
            {
                return Failure(HardwareOperationStatus.Timeout, "rfid_read_timeout", "RFID read timed out.");
            }
        }
        catch (OperationCanceledException) when (execution.IsRemoteCancellationRequested)
        {
            return HandleRemoteCancellation(command, execution);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return Failure(HardwareOperationStatus.Cancelled, "cancelled", "Card present operation was cancelled.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.CommandDeviceUnavailable(command.CommandId, collector.DeviceId, ex);
            return Failure(HardwareOperationStatus.DeviceUnavailable, "device_unavailable", "Collector or RFID reader is not available.");
        }
    }

    private async Task<PostHardwareCommandResultRequest> CollectCardAsync(HardwareCommandEnvelope command, ICollectorDevice collector, CancellationToken stoppingToken)
    {
        if (!TryReadPlaceInCollectorStack(command.Payload, out bool placeInCollectorStack, out PostHardwareCommandResultRequest? invalidPayload))
            return invalidPayload!;

        try
        {
            bool collected = await collector.CollectAsync(placeInCollectorStack, stoppingToken);
            if (!collected)
            {
                logger.CollectorCommandFailed(collector.DeviceId, command.Capability);
                return Failure(HardwareOperationStatus.Failed, "collect_failed", "Collector did not confirm card collect.");
            }

            var result = new JsonObject
            {
                ["collected"] = true,
                ["placeInCollectorStack"] = placeInCollectorStack
            };
            return new PostHardwareCommandResultRequest(HardwareOperationStatus.Succeeded, result, null, timeProvider.GetUtcNow());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.CommandDeviceUnavailable(command.CommandId, collector.DeviceId, ex);
            return Failure(HardwareOperationStatus.DeviceUnavailable, "device_unavailable", "Collector is not available.");
        }
    }

    private async Task<PostHardwareCommandResultRequest> EjectCardAsync(HardwareCommandEnvelope command, ICollectorDevice collector, CancellationToken stoppingToken, ActiveCommandExecution execution)
    {
        try
        {
            using var ejectCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, execution.RemoteCancellation.Token);
            execution.SetPhase("card eject");
            bool ejected = await collector.EjectAsync(ejectCancellation.Token);
            if (!ejected)
            {
                logger.CollectorCommandFailed(collector.DeviceId, command.Capability);
                return Failure(HardwareOperationStatus.Failed, "eject_failed", "Collector did not confirm card eject.");
            }

            using var removalTimeout = new CancellationTokenSource(collector.RemovalTimeout);
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, removalTimeout.Token, execution.RemoteCancellation.Token);

            try
            {
                execution.SetPhase("waiting for collector card removal");
                await collector.WaitForCardRemovalAsync(linkedCancellation.Token);
                var removed = new JsonObject
                {
                    ["removed"] = true,
                    ["recollected"] = false
                };
                return new PostHardwareCommandResultRequest(HardwareOperationStatus.Succeeded, removed, null, timeProvider.GetUtcNow());
            }
            catch (OperationCanceledException) when (removalTimeout.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
            {
                bool recollected = await collector.CollectAsync(placeInCollectorStack: true, stoppingToken);
                var timeoutResult = new JsonObject
                {
                    ["removed"] = false,
                    ["recollected"] = recollected
                };
                return new PostHardwareCommandResultRequest(
                    HardwareOperationStatus.Timeout,
                    timeoutResult,
                    new HardwareErrorResponse("card_removal_timeout", "Card was not removed before timeout."),
                    timeProvider.GetUtcNow());
            }
        }
        catch (OperationCanceledException) when (execution.IsRemoteCancellationRequested)
        {
            return HandleRemoteCancellation(command, execution);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return Failure(HardwareOperationStatus.Cancelled, "cancelled", "Card eject operation was cancelled.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.CommandDeviceUnavailable(command.CommandId, collector.DeviceId, ex);
            return Failure(HardwareOperationStatus.DeviceUnavailable, "device_unavailable", "Collector is not available.");
        }
    }

    private async Task<PostHardwareCommandResultRequest> PresentCardAsync(HardwareCommandEnvelope command, IDispenserDevice dispenser, bool dropAfterRead, CancellationToken stoppingToken, ActiveCommandExecution execution)
    {
        if (!_rfidReaders.TryGetValue(dispenser.RfidReaderDeviceId, out IRfidReaderDevice? rfidReader))
            return Failure(HardwareOperationStatus.DeviceUnavailable, "rfid_reader_unavailable", "Configured RFID reader is not available on this agent.");

        try
        {
            using var dispenseCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, execution.RemoteCancellation.Token);
            execution.SetPhase(dropAfterRead ? "dispense and read" : "waiting for dispenser card present");
            bool dispensed = await dispenser.DispenseAsync(dispenseCancellation.Token);
            if (!dispensed)
            {
                logger.DispenserCommandFailed(dispenser.DeviceId, dropAfterRead ? HardwareCapabilities.CardDispense : HardwareCapabilities.CardPresent);
                return Failure(HardwareOperationStatus.Failed, "dispense_failed", "Dispenser did not confirm card dispense.");
            }

            using var timeout = new CancellationTokenSource(dispenser.RfidReadTimeout);
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token, execution.RemoteCancellation.Token);

            try
            {
                execution.SetPhase("dispenser rfid read");
                string cardNumber = await rfidReader.ReadCardAsync(linkedCancellation.Token);
                if (dropAfterRead && !await DropAfterReadAsync(dispenser, stoppingToken))
                    return Failure(HardwareOperationStatus.Failed, "drop_failed", "Card was read, but dispenser did not confirm card drop.");

                var result = new JsonObject { ["cardNumber"] = cardNumber };
                return new PostHardwareCommandResultRequest(HardwareOperationStatus.Succeeded, result, null, timeProvider.GetUtcNow());
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
            {
                await DropAfterReadAsync(dispenser, stoppingToken);
                return Failure(HardwareOperationStatus.Timeout, "rfid_read_timeout", "RFID read timed out.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await DropAfterReadAsync(dispenser, stoppingToken);
                logger.CardPresentReadFailed(dispenser.DeviceId, rfidReader.DeviceId, ex);
                return Failure(HardwareOperationStatus.Failed, "rfid_read_failed", "RFID read failed.");
            }
        }
        catch (OperationCanceledException) when (execution.IsRemoteCancellationRequested)
        {
            return HandleRemoteCancellation(command, execution);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return Failure(HardwareOperationStatus.Cancelled, "cancelled", "Card operation was cancelled.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.CommandDeviceUnavailable(command.CommandId, dispenser.DeviceId, ex);
            return Failure(HardwareOperationStatus.DeviceUnavailable, "device_unavailable", "Dispenser or RFID reader is not available.");
        }
    }

    private async Task<bool> DropAfterReadAsync(IDispenserDevice dispenser, CancellationToken cancellationToken)
    {
        try
        {
            bool dropped = await dispenser.DropAsync(cancellationToken);
            if (!dropped)
                logger.DispenserCommandFailed(dispenser.DeviceId, HardwareCapabilities.CardDrop);

            return dropped;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.DispenserDropAfterReadFailureFailed(dispenser.DeviceId, ex);
            return false;
        }
    }

    private PostHardwareCommandResultRequest Failure(HardwareOperationStatus status, string code, string message) =>
        new(status, null, new HardwareErrorResponse(code, message), timeProvider.GetUtcNow());

    private void HandleCommandCancelled(HardwareCommandStreamEvent commandEvent)
    {
        if (!_activeCommands.TryGetValue(commandEvent.CommandId, out ActiveCommandExecution? execution))
            return;

        string reason = string.IsNullOrWhiteSpace(commandEvent.Reason) ? "Command was cancelled remotely." : commandEvent.Reason;
        if (!execution.TryCancel(reason))
            return;

        logger.CommandCancellationObserved(commandEvent.CommandId, execution.DeviceId, execution.Capability, reason);
    }

    private async Task ReconcileActiveCommandStatusesAsync(CancellationToken cancellationToken)
    {
        if (_activeCommands.IsEmpty)
            return;

        foreach ((Guid commandId, ActiveCommandExecution execution) in _activeCommands)
        {
            HardwareCommandStatusResponse? status = await gatewayClient.GetCommandStatusAsync(commandId, cancellationToken);
            if (status?.Status != HardwareCommandStatus.Cancelled)
                continue;

            string reason = status.ErrorMessage ?? "Command was cancelled remotely.";
            if (!execution.TryCancel(reason))
                continue;

            logger.CommandCancellationObserved(commandId, execution.DeviceId, execution.Capability, reason);
        }
    }

    private PostHardwareCommandResultRequest HandleRemoteCancellation(HardwareCommandEnvelope command, ActiveCommandExecution execution)
    {
        logger.CommandCancelledDuringPhase(command.CommandId, command.DeviceId, command.Capability, execution.Phase);
        string message = execution.CancellationReason ?? "Command was cancelled remotely.";
        return Failure(HardwareOperationStatus.Cancelled, "cancelled", message);
    }

    private bool TryReadApduRequest(JsonObject? payload, out RfidApduExchangeRequest? request, out PostHardwareCommandResultRequest? error)
    {
        request = null;
        error = null;

        if (payload is null)
        {
            error = Failure(HardwareOperationStatus.Failed, "invalid_payload", "commandHex is required.");
            return false;
        }

        try
        {
            string commandHex = payload["commandHex"]?.GetValue<string>() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(commandHex))
            {
                error = Failure(HardwareOperationStatus.Failed, "invalid_payload", "commandHex is required.");
                return false;
            }

            Convert.FromHexString(commandHex);
            request = new RfidApduExchangeRequest(commandHex);
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            error = Failure(HardwareOperationStatus.Failed, "invalid_payload", "commandHex must be a valid hex string.");
            return false;
        }
    }

    private bool TryReadPlaceInCollectorStack(JsonObject? payload, out bool placeInCollectorStack, out PostHardwareCommandResultRequest? error)
    {
        placeInCollectorStack = true;
        error = null;

        if (payload?["placeInCollectorStack"] is null)
            return true;

        try
        {
            placeInCollectorStack = payload["placeInCollectorStack"]!.GetValue<bool>();
            return true;
        }
        catch (InvalidOperationException)
        {
            error = Failure(HardwareOperationStatus.Failed, "invalid_payload", "placeInCollectorStack must be a boolean value.");
            return false;
        }
    }

    private bool TryReadCardPrintRequest(JsonObject? payload, out string? frontImageBase64, out PostHardwareCommandResultRequest? error)
    {
        frontImageBase64 = null;
        error = null;

        if (payload is null)
        {
            error = Failure(HardwareOperationStatus.Failed, "invalid_payload", "frontImageBase64 is required.");
            return false;
        }

        try
        {
            frontImageBase64 = payload["frontImageBase64"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(frontImageBase64))
            {
                error = Failure(HardwareOperationStatus.Failed, "invalid_payload", "frontImageBase64 is required.");
                return false;
            }

            Convert.FromBase64String(frontImageBase64);
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            error = Failure(HardwareOperationStatus.Failed, "invalid_payload", "frontImageBase64 must be valid base64.");
            return false;
        }
    }

    private static TimeSpan GetReconnectDelay(int failures) => failures switch
    {
        <= 1 => TimeSpan.FromSeconds(1),
        2 => TimeSpan.FromSeconds(2),
        3 => TimeSpan.FromSeconds(5),
        4 => TimeSpan.FromSeconds(10),
        _ => TimeSpan.FromSeconds(30)
    };

    private sealed class ActiveCommandExecution(Guid commandId, string deviceId, string capability) : IDisposable
    {
        private string _phase = "command execution";
        private string? _cancellationReason;

        public Guid CommandId { get; } = commandId;
        public string DeviceId { get; } = deviceId;
        public string Capability { get; } = capability;
        public CancellationTokenSource RemoteCancellation { get; } = new();
        public bool IsRemoteCancellationRequested => RemoteCancellation.IsCancellationRequested;
        public string Phase => _phase;
        public string? CancellationReason => _cancellationReason;

        public void SetPhase(string phase) => _phase = phase;

        public bool TryCancel(string reason)
        {
            _cancellationReason = reason;
            if (RemoteCancellation.IsCancellationRequested)
                return false;

            RemoteCancellation.Cancel();
            return true;
        }

        public void Dispose() => RemoteCancellation.Dispose();
    }
}
