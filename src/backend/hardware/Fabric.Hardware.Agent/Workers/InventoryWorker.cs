using Fabric.Hardware.Agent.Devices;
using Fabric.Hardware.Agent.Gateway;
using Fabric.Hardware.Agent.Options;
using Fabric.Hardware.Contracts.Inventory;
using Microsoft.Extensions.Options;

namespace Fabric.Hardware.Agent;

public sealed class InventoryWorker(
    HardwareGatewayClient gatewayClient,
    IReadOnlyList<IQrReaderDevice> qrReaders,
    IReadOnlyList<IDispenserDevice> dispensers,
    IReadOnlyList<ICollectorDevice> collectors,
    IReadOnlyList<IEncoderDevice> encoders,
    IReadOnlyList<IEidReaderDevice> eidReaders,
    IReadOnlyList<IRfidReaderDevice> rfidReaders,
    TimeProvider timeProvider,
    IOptions<HardwareAgentOptions> options,
    ILogger<InventoryWorker> logger) : BackgroundService
{
    private readonly HardwareAgentOptions _options = options.Value;
    private readonly InventorySyncState _syncState = new(options.Value.InventoryInterval);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.WorkerStarted(nameof(InventoryWorker));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                DateTimeOffset reportedAt = timeProvider.GetUtcNow();
                HardwareDeviceInventoryItem[] devices =
                [
                    .. qrReaders.Select(qrReader => qrReader.GetInventoryItem()),
                    .. dispensers.Select(dispenser => dispenser.GetInventoryItem()),
                    .. collectors.Select(collector => collector.GetInventoryItem()),
                    .. encoders.Select(encoder => encoder.GetInventoryItem()),
                    .. eidReaders.Select(eidReader => eidReader.GetInventoryItem()),
                    .. rfidReaders.Select(rfidReader => rfidReader.GetInventoryItem())
                ];

                InventorySyncDecision decision = _syncState.GetDecision(devices, reportedAt);
                if (decision.ShouldSend)
                {
                    var request = new PostHardwareInventoryRequest(reportedAt, devices);
                    await gatewayClient.PostInventoryAsync(request, stoppingToken);
                    _syncState.MarkSent(decision, reportedAt);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.InventoryFailed(ex);
            }

            await Task.Delay(_options.HeartbeatInterval, stoppingToken);
        }

        logger.WorkerStopped(nameof(InventoryWorker));
    }
}
