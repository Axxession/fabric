using System.Text.Json;
using System.Text.Json.Nodes;
using Fabric.Hardware.Contracts;
using Fabric.Hardware.Contracts.Capabilities;
using Fabric.Hardware.Contracts.Cards;
using Fabric.Hardware.Contracts.Commands;
using Fabric.Hardware.Desfire.Encoding.Specifications;
using Fabric.Hardware.Desfire.Scripting;
using Fabric.Hardware.Desfire.Scripting.Contracts;
using Fabric.Hardware.Desfire.Scripting.Entities;
using Fabric.Server.Desfire.Contracts;
using Fabric.Server.Desfire.Domain;
using Fabric.Server.Desfire.Persistence;
using Fabric.Server.Hardware.Application;
using Fabric.Server.Hardware.Contracts;
using Fabric.Server.Hardware.Domain;
using Fabric.Server.Hardware.Persistence;
using Fabric.Server.Printing.Application;
using Fabric.Server.Printing.Domain;
using Fabric.Server.Printing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Desfire.Application;

public sealed class DesfireEncodingService(
    DesfireDbContext desfireDb,
    HardwareDbContext hardwareDb,
    PrintingDbContext printingDb,
    DesfireTransformationPlanner planner,
    DesfireKeyGroupResolver keyGroupResolver,
    DesfireDeviceLeaseStore leaseStore,
    DesfireVariableResolver variableResolver,
    DesfireEncodingWakeChannel wakeChannel,
    HardwareCommandStore commandStore,
    HardwareAgentConnectionManager connectionManager,
    HardwareConnectionStatusResolver connectionStatusResolver,
    PrintDesignParser printDesignParser,
    RenderServiceFactory renderServiceFactory,
    TimeProvider timeProvider,
    ILogger<DesfireEncodingService> logger)
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ClaimDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CardWorkflowCommandTimeout = TimeSpan.FromMinutes(5);

    public async Task<DesfireEncodingResult> CreateAdHocAsync(CreateBadgeJobRequest request, CancellationToken ct)
    {
        DesfireEncodingResult created = await CreateSingleRunAsync(request, ct);
        if (created.Failure is not null || request.Mode == BadgeJobMode.Queued)
        {
            if (created.Failure is null && request.Mode == BadgeJobMode.Queued)
                wakeChannel.Signal();

            return created;
        }

        BadgeJob run = created.Run!;
        if (string.IsNullOrWhiteSpace(request.AgentId) || string.IsNullOrWhiteSpace(request.DeviceId))
        {
            run.Fail(BadgeJobStatus.Failed, "Sync badge job requires agentId and deviceId.", "[]", timeProvider.GetUtcNow());
            await desfireDb.SaveChangesAsync(ct);
            return new DesfireEncodingResult(run, Results.Problem(run.ErrorMessage, statusCode: StatusCodes.Status400BadRequest));
        }

        return await ExecuteRunAsync(run.Id, request.AgentId, request.DeviceId, requeueOnTransientFailure: false, ct);
    }

    public async Task<DesfireEncodingResult> CreateSingleRunAsync(CreateBadgeJobRequest request, CancellationToken ct)
    {
        if (request.TransformationId is null && request.PrintDesignId is null)
            return new DesfireEncodingResult(null, Results.Problem("Badge job must specify transformation, print design, or both.", statusCode: StatusCodes.Status400BadRequest));

        Transformation? transformation = null;
        if (request.TransformationId is not null)
        {
            transformation = await desfireDb.Transformations.SingleOrDefaultAsync(candidate => candidate.Id == request.TransformationId, ct);
            if (transformation is null)
                return DesfireEncodingResult.NotFound;
        }

        if (request.PrintDesignId is not null)
        {
            PrintDesign? printDesign = await printingDb.PrintDesigns.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == request.PrintDesignId, ct);
            if (printDesign is null)
                return new DesfireEncodingResult(null, Results.Problem("Print design does not exist.", statusCode: StatusCodes.Status409Conflict));

            if (printDesign.SurfaceKind != PrintSurfaceKind.Card)
                return new DesfireEncodingResult(null, Results.Problem("Print design must target card surface.", statusCode: StatusCodes.Status409Conflict));
        }

        DesfireEncoder? encoder = null;
        string? agentId = request.AgentId;
        string? deviceId = request.DeviceId;
        if (request.EncoderId is not null)
        {
            encoder = await desfireDb.Encoders.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == request.EncoderId, ct);
            if (encoder is null)
                return new DesfireEncodingResult(null, Results.Problem("Encoder does not exist.", statusCode: StatusCodes.Status409Conflict));

            if (!encoder.Enabled)
                return new DesfireEncodingResult(null, Results.Problem("Encoder is disabled.", statusCode: StatusCodes.Status409Conflict));

            if (request.TransformationId is not null && !encoder.SupportsEncoding)
                return new DesfireEncodingResult(null, Results.Problem("Selected encoder does not support encoding.", statusCode: StatusCodes.Status409Conflict));

            if (request.PrintDesignId is not null && !encoder.SupportsPrinting)
                return new DesfireEncodingResult(null, Results.Problem("Selected encoder does not support card printing.", statusCode: StatusCodes.Status409Conflict));

            agentId ??= encoder.AgentId;
            deviceId ??= encoder.DeviceId;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        BadgeJob run = BadgeJob.Create(
            request.TransformationId,
            null,
            request.EncoderId,
            request.PrintDesignId,
            request.KioskSessionId,
            BadgeJobKind.Single,
            request.Source,
            JsonSerializer.Serialize(request.Input, DesfireJson.Options),
            transformation?.VariableConfigsJson ?? "[]",
            agentId,
            deviceId,
            request.Priority,
            now);
        desfireDb.BadgeJobs.Add(run);
        await desfireDb.SaveChangesAsync(ct);
        return new DesfireEncodingResult(run, null);
    }

    public async Task<BadgeJob?> TryClaimNextRunForDeviceAsync(string agentId, string deviceId, string workerId, CancellationToken ct)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        string normalizedAgentId = agentId.Trim().ToLowerInvariant();
        string normalizedDeviceId = deviceId.Trim().ToLowerInvariant();
        HardwareDevice? device = await hardwareDb.Devices
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.AgentId == normalizedAgentId && candidate.DeviceId == normalizedDeviceId, ct);
        if (device is null)
            return null;

        bool supportsEncoding = SupportsFullEncodingWorkflow(device.Capabilities);
        bool supportsPrinting = device.Capabilities.Contains(HardwareCapabilities.CardPrint, StringComparer.OrdinalIgnoreCase);

        Guid? runId = await desfireDb.BadgeJobs
            .AsNoTracking()
            .Where(candidate => (candidate.Status == BadgeJobStatus.Pending || (candidate.Status == BadgeJobStatus.Claimed && candidate.ClaimExpiresAt <= now))
                && (candidate.RequestedAgentId == null || candidate.RequestedAgentId == normalizedAgentId)
                && (candidate.RequestedDeviceId == null || candidate.RequestedDeviceId == normalizedDeviceId)
                && (supportsEncoding || candidate.TransformationId == null)
                && (supportsPrinting || candidate.PrintDesignId == null))
            .OrderByDescending(candidate => candidate.RequestedAgentId == normalizedAgentId && candidate.RequestedDeviceId == normalizedDeviceId)
            .ThenByDescending(candidate => candidate.Priority)
            .ThenBy(candidate => candidate.RequestedAt)
            .Select(candidate => (Guid?)candidate.Id)
            .FirstOrDefaultAsync(ct);

        if (runId is null)
        {
            logger.DesfireEncodingNoClaimableRun(normalizedAgentId, normalizedDeviceId, workerId);
            return null;
        }

        DateTimeOffset expiresAt = now.Add(ClaimDuration);
        int updated = await desfireDb.BadgeJobs
            .Where(candidate => candidate.Id == runId
                && (candidate.Status == BadgeJobStatus.Pending || (candidate.Status == BadgeJobStatus.Claimed && candidate.ClaimExpiresAt <= now))
                && (candidate.RequestedAgentId == null || candidate.RequestedAgentId == normalizedAgentId)
                && (candidate.RequestedDeviceId == null || candidate.RequestedDeviceId == normalizedDeviceId)
                && (supportsEncoding || candidate.TransformationId == null)
                && (supportsPrinting || candidate.PrintDesignId == null))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(candidate => candidate.Status, BadgeJobStatus.Claimed)
                .SetProperty(candidate => candidate.ClaimedBy, workerId)
                .SetProperty(candidate => candidate.ClaimedAt, now)
                .SetProperty(candidate => candidate.ClaimExpiresAt, expiresAt)
                .SetProperty(candidate => candidate.AttemptCount, candidate => candidate.AttemptCount + 1), ct);

        if (updated == 0)
        {
            logger.DesfireEncodingClaimLost(runId.Value, normalizedAgentId, normalizedDeviceId, workerId);
            return null;
        }

        logger.DesfireEncodingRunClaimed(runId.Value, normalizedAgentId, normalizedDeviceId, workerId, expiresAt);

        return await desfireDb.BadgeJobs.AsNoTracking().SingleAsync(candidate => candidate.Id == runId, ct);
    }

    public async Task<DesfireEncodingResult> ExecuteRunAsync(Guid runId, string agentId, string deviceId, bool requeueOnTransientFailure, CancellationToken ct)
        => await ExecuteRunAsync(runId, agentId, deviceId, requeueOnTransientFailure, onPhaseChanged: null, ct);

    public async Task<DesfireEncodingResult> ExecuteRunAsync(Guid runId, string agentId, string deviceId, bool requeueOnTransientFailure, Func<DesfireEncodingPhase, CancellationToken, Task>? onPhaseChanged, CancellationToken ct)
    {
        BadgeJob? run = await desfireDb.BadgeJobs.SingleOrDefaultAsync(candidate => candidate.Id == runId, ct);
        if (run is null)
            return DesfireEncodingResult.NotFound;

        if (run.Status == BadgeJobStatus.Cancelled)
            return new DesfireEncodingResult(run, Results.Problem(run.ErrorMessage, statusCode: StatusCodes.Status409Conflict));

        IResult? deviceFailure = await ValidateDeviceAsync(agentId, deviceId, requireEncoding: run.TransformationId is not null, requirePrinting: run.PrintDesignId is not null, ct);
        if (deviceFailure is not null)
        {
            if (requeueOnTransientFailure)
            {
                logger.DesfireEncodingRunRequeued(run.Id, agentId, deviceId, "device_unavailable_or_offline");
                run.Requeue("Hardware device is unavailable or not ready.", timeProvider.GetUtcNow());
                await desfireDb.SaveChangesAsync(ct);
                wakeChannel.Signal();
                return new DesfireEncodingResult(run, deviceFailure);
            }

            logger.DesfireEncodingRunDeviceUnavailable(run.Id, agentId, deviceId, "device_unavailable_or_offline");
            run.Fail(BadgeJobStatus.DeviceUnavailable, "Hardware device is unavailable or not ready.", "[]", timeProvider.GetUtcNow());
            await desfireDb.SaveChangesAsync(ct);
            return new DesfireEncodingResult(run, deviceFailure);
        }

        DesfireDeviceLeaseResult leaseResult = await leaseStore.TryAcquireAsync(agentId, deviceId, run.Id, LeaseDuration, ct);
        if (leaseResult == DesfireDeviceLeaseResult.Busy)
        {
            if (requeueOnTransientFailure)
            {
                logger.DesfireEncodingRunRequeued(run.Id, agentId, deviceId, "device_busy");
                run.Requeue("Hardware device is busy.", timeProvider.GetUtcNow());
                await desfireDb.SaveChangesAsync(ct);
                wakeChannel.Signal();
                return new DesfireEncodingResult(run, Results.Problem("Hardware device is busy.", statusCode: StatusCodes.Status409Conflict));
            }

            logger.DesfireEncodingRunDeviceUnavailable(run.Id, agentId, deviceId, "device_busy");
            run.Fail(BadgeJobStatus.DeviceUnavailable, "Hardware device is busy.", "[]", timeProvider.GetUtcNow());
            await desfireDb.SaveChangesAsync(ct);
            return new DesfireEncodingResult(run, Results.Problem("Hardware device is busy.", statusCode: StatusCodes.Status409Conflict));
        }

        try
        {
            logger.DesfireEncodingRunStarting(run.Id, agentId, deviceId);
            run.Start(agentId, deviceId, timeProvider.GetUtcNow());
            await desfireDb.SaveChangesAsync(ct);

            bool requiresEncoding = run.TransformationId is not null;
            bool requiresPrinting = run.PrintDesignId is not null;
            Transformation? transformation = null;
            ExecutionPlan? plan = null;
            ResolvedDesfireVariables resolvedVariables = new([], "[]");
            string? cardUid = null;

            if (requiresEncoding)
            {
                transformation = await desfireDb.Transformations.SingleOrDefaultAsync(candidate => candidate.Id == run.TransformationId, ct);
                if (transformation is null)
                    throw new InvalidOperationException("Transformation does not exist.");

                TemplateSpecification current = await ResolveSourceAsync(transformation, ct);
                TemplateSpecification target = await planner.ResolveLatestDesignAsync(transformation.ToChipDesignName, ct);
                plan = await ChipDesignTransformer.CreatePlan(keyGroupResolver, current, target, readUid: true);
                if (plan.Errors.Count > 0)
                {
                    string error = string.Join("; ", plan.Errors.Select(scriptError => scriptError.Message));
                    run.Fail(BadgeJobStatus.Failed, error, "[]", timeProvider.GetUtcNow());
                    await desfireDb.SaveChangesAsync(ct);
                    return new DesfireEncodingResult(run, Results.Problem(error, statusCode: StatusCodes.Status409Conflict));
                }

                IReadOnlyList<TransformationVariableConfigRequest> variableConfigs = EnsureVariableConfigs(plan.RequiredProviders, JsonSerializer.Deserialize<TransformationVariableConfigRequest[]>(run.VariableConfigJson, DesfireJson.Options) ?? []);
                resolvedVariables = await variableResolver.ResolveAsync(plan.RequiredProviders, variableConfigs, run.InputJson, ct);
            }

            List<CommandAuditItem> audit = [];
            await NotifyPhaseChangedAsync(onPhaseChanged, DesfireEncodingPhase.WaitingForCard, ct);
            PostHardwareCommandResultRequest presentResult = await ExecuteHardwareCommandAsync(run.Id, agentId, deviceId, HardwareCapabilities.CardPresent, payload: null, ct);
            if (presentResult.Status == HardwareOperationStatus.Cancelled)
                return await CancelRunAsync(run, "Badge job cancelled while waiting for card.", JsonSerializer.Serialize(audit, DesfireJson.Options), ct);

            AddHardwareAuditItem(audit, HardwareCapabilities.CardPresent, presentResult);
            if (presentResult.Status != HardwareOperationStatus.Succeeded)
            {
                run.Fail(BadgeJobStatus.Failed, presentResult.Error?.Message ?? "Card present operation failed.", JsonSerializer.Serialize(audit, DesfireJson.Options), timeProvider.GetUtcNow());
                await desfireDb.SaveChangesAsync(ct);
                return new DesfireEncodingResult(run, Results.Problem(run.ErrorMessage, statusCode: StatusCodes.Status409Conflict));
            }

            if (requiresEncoding)
                await NotifyPhaseChangedAsync(onPhaseChanged, DesfireEncodingPhase.Encoding, ct);

            if (plan is not null)
            {
                plan.OnCommandExecuted += (_, args) => audit.Add(new CommandAuditItem(args.Index, args.Operation.ToString() ?? args.Operation.GetType().Name, args.Response.IsSuccess, args.Response.StatusCode.ToString(), args.CardUid));
                using HardwareRfidEncoder encoder = new(commandStore, connectionManager, new HardwareDeviceRef(agentId, deviceId), run.Id, logger);
                ExecutedEncodingPlan executed = await plan.Execute(logger, resolvedVariables.Variables, encoder, ct);
                cardUid = executed.CardUid;
                if (!executed.IsSuccess)
                {
                    PostHardwareCommandResultRequest ejectAfterFailure = await ExecuteHardwareCommandAsync(run.Id, agentId, deviceId, HardwareCapabilities.CardEject, payload: null, ct);
                    if (ejectAfterFailure.Status == HardwareOperationStatus.Cancelled)
                        return await CancelRunAsync(run, "Badge job cancelled during failure cleanup.", JsonSerializer.Serialize(audit, DesfireJson.Options), ct);

                    AddHardwareAuditItem(audit, HardwareCapabilities.CardEject, ejectAfterFailure);
                    string auditJsonFailure = JsonSerializer.Serialize(audit, DesfireJson.Options);
                    string errorMessage = executed.ErrorMessage ?? "Encoding failed.";
                    if (ejectAfterFailure.Status != HardwareOperationStatus.Succeeded)
                        errorMessage = $"{errorMessage} Card eject failed: {ejectAfterFailure.Error?.Message ?? ejectAfterFailure.Status.ToString()}.";

                    run.Fail(BadgeJobStatus.Failed, errorMessage, auditJsonFailure, timeProvider.GetUtcNow(), executed.CardUid);
                    await desfireDb.SaveChangesAsync(ct);
                    return new DesfireEncodingResult(run, Results.Problem(run.ErrorMessage, statusCode: StatusCodes.Status409Conflict));
                }
            }

            string auditJson = JsonSerializer.Serialize(audit, DesfireJson.Options);

            if (requiresPrinting)
            {
                PrintCardResponse printResult = await PrintBadgeAsync(run, agentId, deviceId, resolvedVariables, ct);
                AddHardwareAuditItem(audit, HardwareCapabilities.CardPrint, printResult.Status, printResult.Error?.Message);
                auditJson = JsonSerializer.Serialize(audit, DesfireJson.Options);
                if (printResult.Status != HardwareOperationStatus.Succeeded)
                {
                    PostHardwareCommandResultRequest ejectAfterPrintFailure = await ExecuteHardwareCommandAsync(run.Id, agentId, deviceId, HardwareCapabilities.CardEject, payload: null, ct);
                    AddHardwareAuditItem(audit, HardwareCapabilities.CardEject, ejectAfterPrintFailure);
                    auditJson = JsonSerializer.Serialize(audit, DesfireJson.Options);
                    string errorMessage = printResult.Error?.Message ?? "Card print operation failed.";
                    if (ejectAfterPrintFailure.Status != HardwareOperationStatus.Succeeded)
                        errorMessage = $"{errorMessage} Card eject failed: {ejectAfterPrintFailure.Error?.Message ?? ejectAfterPrintFailure.Status.ToString()}.";

                    run.Fail(BadgeJobStatus.Failed, errorMessage, auditJson, timeProvider.GetUtcNow(), cardUid);
                    await desfireDb.SaveChangesAsync(ct);
                    return new DesfireEncodingResult(run, Results.Problem(run.ErrorMessage, statusCode: StatusCodes.Status409Conflict));
                }
            }

            await NotifyPhaseChangedAsync(onPhaseChanged, DesfireEncodingPhase.WaitingForRemoval, ct);

            PostHardwareCommandResultRequest ejectResult = await ExecuteHardwareCommandAsync(run.Id, agentId, deviceId, HardwareCapabilities.CardEject, payload: null, ct);
            if (ejectResult.Status == HardwareOperationStatus.Cancelled)
                return await CancelRunAsync(run, "Badge job cancelled while waiting for card removal.", JsonSerializer.Serialize(audit, DesfireJson.Options), ct);

            AddHardwareAuditItem(audit, HardwareCapabilities.CardEject, ejectResult);
            auditJson = JsonSerializer.Serialize(audit, DesfireJson.Options);
            if (ejectResult.Status != HardwareOperationStatus.Succeeded)
            {
                run.Fail(BadgeJobStatus.Failed, ejectResult.Error?.Message ?? "Card eject operation failed.", auditJson, timeProvider.GetUtcNow(), cardUid);
                await desfireDb.SaveChangesAsync(ct);
                return new DesfireEncodingResult(run, Results.Problem(run.ErrorMessage, statusCode: StatusCodes.Status409Conflict));
            }

            run.Complete(
                cardUid,
                resolvedVariables.AuditJson,
                JsonSerializer.Serialize(new { encode = requiresEncoding, print = requiresPrinting, operationCount = plan?.Operations.Count ?? 0, requiredVariables = plan?.RequiredProviders ?? Array.Empty<string>() }, DesfireJson.Options),
                auditJson,
                timeProvider.GetUtcNow());
            await desfireDb.SaveChangesAsync(ct);
            logger.DesfireEncodingRunCompleted(run.Id, agentId, deviceId, cardUid);
            return new DesfireEncodingResult(run, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.DesfireEncodingRunCancelled(run.Id, agentId, deviceId);
            return await CancelRunAsync(run, "Encoding run cancelled.", run.CommandAuditJson, CancellationToken.None);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.DesfireEncodingRunCancelled(run.Id, agentId, deviceId);
            return await CancelRunAsync(run, "Encoding run cancelled.", run.CommandAuditJson, CancellationToken.None);
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            logger.DesfireEncodingRunFailed(run.Id, agentId, deviceId, ex);
            run.Fail(BadgeJobStatus.Failed, ex.Message, "[]", timeProvider.GetUtcNow());
            await desfireDb.SaveChangesAsync(ct);
            return new DesfireEncodingResult(run, Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict));
        }
        finally
        {
            await leaseStore.ReleaseAsync(agentId, deviceId, run.Id, CancellationToken.None);
        }
    }

    public async Task<List<HardwareDevice>> GetSchedulableEncodingDevicesAsync(CancellationToken ct)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        List<HardwareDevice> devices = await hardwareDb.Devices
            .AsNoTracking()
            .Where(device => device.Enabled)
            .ToListAsync(ct);
        Dictionary<string, HardwareAgent> agentsById = await hardwareDb.Agents
            .AsNoTracking()
            .ToDictionaryAsync(agent => agent.Id, StringComparer.OrdinalIgnoreCase, ct);

        List<DesfireDeviceLease> activeLeases = await desfireDb.DeviceLeases
            .AsNoTracking()
            .Where(lease => lease.ReleasedAt == null && lease.ExpiresAt > now)
            .ToListAsync(ct);

        List<HardwareDevice> schedulableDevices = [];
        foreach (HardwareDevice device in devices)
        {
            agentsById.TryGetValue(device.AgentId, out HardwareAgent? agent);
            if (connectionStatusResolver.GetStatus(agent?.LastSeenAt) == HardwareConnectionStatus.Offline)
                continue;

            if (!string.Equals(device.State, "online", StringComparison.OrdinalIgnoreCase))
            {
                logger.DesfireEncodingDeviceSkippedState(device.AgentId, device.DeviceId, device.State);
                continue;
            }

            if (!SupportsAnyBadgeWorkflow(device.Capabilities))
            {
                logger.DesfireEncodingDeviceSkippedCapabilities(device.AgentId, device.DeviceId);
                continue;
            }

            if (activeLeases.Any(lease => lease.AgentId == device.AgentId && lease.DeviceId == device.DeviceId))
            {
                logger.DesfireEncodingDeviceSkippedBusy(device.AgentId, device.DeviceId);
                continue;
            }

            schedulableDevices.Add(device);
        }

        logger.DesfireEncodingSchedulableDevices(devices.Count, activeLeases.Count, schedulableDevices.Count);
        return schedulableDevices;
    }

    private async Task<TemplateSpecification> ResolveSourceAsync(Transformation transformation, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(transformation.FromChipDesignName))
            return await planner.ResolveLatestDesignAsync(transformation.FromChipDesignName, ct);

        return new TemplateSpecification
        {
            Picc = new PiccSpecification
            {
                Key = new KeySpecification
                {
                    KeyGroup = "_blank_",
                    KeyGroupName = "_blank_",
                    KeySet = 0,
                    Key = 0
                }
            },
            Applications = []
        };
    }

    private async Task<IResult?> ValidateDeviceAsync(string agentId, string deviceId, bool requireEncoding, bool requirePrinting, CancellationToken ct)
    {
        string normalizedAgentId = HardwareAgent.NormalizeId(agentId);
        string normalizedDeviceId = HardwareDevice.NormalizeDeviceId(deviceId);
        HardwareAgent? agent = await hardwareDb.Agents.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == normalizedAgentId, ct);
        if (agent is null)
            return Results.NotFound();

        if (connectionStatusResolver.GetStatus(agent.LastSeenAt) == HardwareConnectionStatus.Offline)
            return Results.Problem("Hardware agent is offline.", statusCode: StatusCodes.Status409Conflict);

        HardwareDevice? device = await hardwareDb.Devices.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.AgentId == normalizedAgentId && candidate.DeviceId == normalizedDeviceId, ct);
        if (device is null)
            return Results.NotFound();

        if (!device.Enabled)
            return Results.Problem("Hardware device is disabled.", statusCode: StatusCodes.Status409Conflict);

        if (requireEncoding && !SupportsFullEncodingWorkflow(device.Capabilities))
            return Results.Problem("Hardware device does not support full RFID encoding workflow.", statusCode: StatusCodes.Status409Conflict);

        if (requirePrinting && !device.Capabilities.Contains(HardwareCapabilities.CardPrint, StringComparer.OrdinalIgnoreCase))
            return Results.Problem("Hardware device does not support card printing.", statusCode: StatusCodes.Status409Conflict);

        if (!requireEncoding && !requirePrinting && !SupportsAnyBadgeWorkflow(device.Capabilities))
            return Results.Problem("Hardware device does not support badge workflow.", statusCode: StatusCodes.Status409Conflict);

        if (!string.Equals(device.State, "online", StringComparison.OrdinalIgnoreCase))
        {
            logger.DesfireEncodingDeviceValidationFailed(normalizedAgentId, normalizedDeviceId, device.State, device.Enabled);
            return Results.Problem("Hardware device is not ready.", statusCode: StatusCodes.Status409Conflict);
        }

        return null;
    }

    private static TransformationVariableConfigRequest[] EnsureVariableConfigs(IReadOnlyList<string> requiredVariables, IReadOnlyList<TransformationVariableConfigRequest> variableConfigs)
    {
        Dictionary<string, TransformationVariableConfigRequest> configsByName = variableConfigs
            .Where(config => !string.IsNullOrWhiteSpace(config.Name))
            .GroupBy(config => config.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        return [.. requiredVariables.Select(variable => configsByName.GetValueOrDefault(variable) ?? new TransformationVariableConfigRequest(variable, TransformationVariableKind.UserProvided, new VariableFormatRequest(DesfireVariableFormatKind.Hex), Field: variable))];
    }

    private static async Task NotifyPhaseChangedAsync(Func<DesfireEncodingPhase, CancellationToken, Task>? onPhaseChanged, DesfireEncodingPhase phase, CancellationToken cancellationToken)
    {
        if (onPhaseChanged is null)
            return;

        await onPhaseChanged(phase, cancellationToken);
    }

    private async Task<PrintCardResponse> PrintBadgeAsync(BadgeJob run, string agentId, string deviceId, ResolvedDesfireVariables resolvedVariables, CancellationToken cancellationToken)
    {
        PrintDesign design = await printingDb.PrintDesigns
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == run.PrintDesignId, cancellationToken)
            ?? throw new InvalidOperationException("Print design does not exist.");

        if (design.SurfaceKind != PrintSurfaceKind.Card)
            throw new InvalidOperationException("Print design must target card surface.");

        if (!printDesignParser.TryParse(design.DesignJson, out PrintTemplate? template, out string? parseError) || template is null)
            throw new InvalidOperationException(parseError ?? "Print design JSON is invalid.");

        IReadOnlyDictionary<string, string> printData = BuildPrintData(run.InputJson, resolvedVariables.Variables);
        var renderProfile = new RenderProfile
        {
            Target = RenderTarget.BmpImage,
            Dpi = design.DefaultRenderProfile?.Dpi ?? RenderProfileResolver.Fallback.Dpi,
            Background = design.DefaultRenderProfile?.Background ?? RenderProfileResolver.Fallback.Background,
            Quality = design.DefaultRenderProfile?.Quality
        };

        IRenderService renderService = renderServiceFactory.Create(renderProfile);
        RenderedDocument document = await renderService.RenderAsync(printData, template, cancellationToken);
        PendingHardwareCommand command = commandStore.Create(
            agentId,
            deviceId,
            HardwareCapabilities.CardPrint,
            new JsonObject { ["frontImageBase64"] = Convert.ToBase64String(document.Content) },
            CardWorkflowCommandTimeout,
            run.Id);

        logger.DesfireEncodingHardwareCommandQueued(run.Id, agentId, deviceId, HardwareCapabilities.CardPrint, command.CommandId);
        connectionManager.NotifyCommandAvailable(agentId, command.CommandId);
        logger.DesfireEncodingHardwareCommandNotified(run.Id, agentId, deviceId, HardwareCapabilities.CardPrint, command.CommandId);
        PostHardwareCommandResultRequest result = await commandStore.WaitForResultAsync(command, cancellationToken);
        logger.DesfireEncodingHardwareCommandCompleted(run.Id, agentId, deviceId, HardwareCapabilities.CardPrint, command.CommandId, result.Status);
        return new PrintCardResponse(result.Status, result.Error);
    }

    private async Task<PostHardwareCommandResultRequest> ExecuteHardwareCommandAsync(Guid runId, string agentId, string deviceId, string capability, JsonObject? payload, CancellationToken cancellationToken)
    {
        PendingHardwareCommand command = commandStore.Create(agentId, deviceId, capability, payload, CardWorkflowCommandTimeout, runId);
        logger.DesfireEncodingHardwareCommandQueued(runId, agentId, deviceId, capability, command.CommandId);
        connectionManager.NotifyCommandAvailable(agentId, command.CommandId);
        logger.DesfireEncodingHardwareCommandNotified(runId, agentId, deviceId, capability, command.CommandId);
        PostHardwareCommandResultRequest result = await commandStore.WaitForResultAsync(command, cancellationToken);
        logger.DesfireEncodingHardwareCommandCompleted(runId, agentId, deviceId, capability, command.CommandId, result.Status);
        return result;
    }

    public async Task<int> CancelRunsForKioskSessionAsync(Guid kioskSessionId, CancellationToken ct)
    {
        BadgeJob[] runs = await desfireDb.BadgeJobs
            .Where(run => run.KioskSessionId == kioskSessionId && (run.Status == BadgeJobStatus.Pending || run.Status == BadgeJobStatus.Claimed || run.Status == BadgeJobStatus.Running))
            .ToArrayAsync(ct);

        if (runs.Length == 0)
            return 0;

        DateTimeOffset now = timeProvider.GetUtcNow();
        foreach (BadgeJob run in runs)
        {
            if (run.Status == BadgeJobStatus.Running)
                commandStore.CancelByOwner(run.Id, "Badge job cancelled.");

            if (run.Status != BadgeJobStatus.Running)
                run.Cancel("Badge job cancelled.", run.CommandAuditJson, now);
        }

        await desfireDb.SaveChangesAsync(ct);
        return runs.Length;
    }

    private async Task<DesfireEncodingResult> CancelRunAsync(BadgeJob run, string message, string commandAuditJson, CancellationToken ct)
    {
        run.Cancel(message, commandAuditJson, timeProvider.GetUtcNow());
        await desfireDb.SaveChangesAsync(ct);
        return new DesfireEncodingResult(run, Results.Problem(run.ErrorMessage, statusCode: StatusCodes.Status409Conflict));
    }

    private static void AddHardwareAuditItem(List<CommandAuditItem> audit, string operation, PostHardwareCommandResultRequest result)
    {
        string? cardNumber = result.Result?["cardNumber"]?.GetValue<string>();
        audit.Add(new CommandAuditItem(audit.Count, operation, result.Status == HardwareOperationStatus.Succeeded, result.Status.ToString(), cardNumber));
    }

    private static void AddHardwareAuditItem(List<CommandAuditItem> audit, string operation, HardwareOperationStatus status, string? detail)
    {
        audit.Add(new CommandAuditItem(audit.Count, operation, status == HardwareOperationStatus.Succeeded, status.ToString(), detail));
    }

    private static Dictionary<string, string> BuildPrintData(string inputJson, IReadOnlyDictionary<string, byte[]> resolvedVariables)
    {
        Dictionary<string, string> data = new(StringComparer.OrdinalIgnoreCase);

        using JsonDocument document = JsonDocument.Parse(inputJson);
        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Null || property.Value.ValueKind == JsonValueKind.Undefined)
                    continue;

                data[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    JsonValueKind.True => bool.TrueString,
                    JsonValueKind.False => bool.FalseString,
                    _ => property.Value.ToString()
                };
            }
        }

        foreach ((string key, byte[] value) in resolvedVariables)
            data[key] = Convert.ToHexString(value);

        return data;
    }

    private static bool SupportsFullEncodingWorkflow(IReadOnlyList<string> capabilities) =>
        capabilities.Contains(HardwareCapabilities.CardPresent, StringComparer.OrdinalIgnoreCase)
        && capabilities.Contains(HardwareCapabilities.RfidApduExchange, StringComparer.OrdinalIgnoreCase)
        && capabilities.Contains(HardwareCapabilities.CardEject, StringComparer.OrdinalIgnoreCase);

    private static bool SupportsPrintOnlyWorkflow(IReadOnlyList<string> capabilities) =>
        capabilities.Contains(HardwareCapabilities.CardPresent, StringComparer.OrdinalIgnoreCase)
        && capabilities.Contains(HardwareCapabilities.CardPrint, StringComparer.OrdinalIgnoreCase)
        && capabilities.Contains(HardwareCapabilities.CardEject, StringComparer.OrdinalIgnoreCase);

    private static bool SupportsAnyBadgeWorkflow(IReadOnlyList<string> capabilities) =>
        SupportsFullEncodingWorkflow(capabilities) || SupportsPrintOnlyWorkflow(capabilities);
}

internal static partial class DesfireEncodingServiceLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Desfire encoding found no claimable run for device {AgentId}/{DeviceId} on worker {WorkerId}")]
    public static partial void DesfireEncodingNoClaimableRun(this ILogger logger, string agentId, string deviceId, string workerId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Desfire encoding lost claim for run {RunId} on device {AgentId}/{DeviceId} for worker {WorkerId}")]
    public static partial void DesfireEncodingClaimLost(this ILogger logger, Guid runId, string agentId, string deviceId, string workerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Desfire encoding claimed run {RunId} on device {AgentId}/{DeviceId} for worker {WorkerId} until {ExpiresAt}")]
    public static partial void DesfireEncodingRunClaimed(this ILogger logger, Guid runId, string agentId, string deviceId, string workerId, DateTimeOffset expiresAt);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Desfire encoding skipped device {AgentId}/{DeviceId} because state is {State}")]
    public static partial void DesfireEncodingDeviceSkippedState(this ILogger logger, string agentId, string deviceId, string state);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Desfire encoding skipped device {AgentId}/{DeviceId} because required capabilities are missing")]
    public static partial void DesfireEncodingDeviceSkippedCapabilities(this ILogger logger, string agentId, string deviceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Desfire encoding skipped device {AgentId}/{DeviceId} because an active lease exists")]
    public static partial void DesfireEncodingDeviceSkippedBusy(this ILogger logger, string agentId, string deviceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Desfire encoding evaluated {DeviceCount} enabled devices, {LeaseCount} active leases, {SchedulableDeviceCount} schedulable devices")]
    public static partial void DesfireEncodingSchedulableDevices(this ILogger logger, int deviceCount, int leaseCount, int schedulableDeviceCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Desfire encoding validation failed for device {AgentId}/{DeviceId}: enabled={Enabled}, state={State}")]
    public static partial void DesfireEncodingDeviceValidationFailed(this ILogger logger, string agentId, string deviceId, string state, bool enabled);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Desfire encoding requeued run {RunId} on device {AgentId}/{DeviceId} because {Reason}")]
    public static partial void DesfireEncodingRunRequeued(this ILogger logger, Guid runId, string agentId, string deviceId, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Desfire encoding marked run {RunId} device-unavailable on device {AgentId}/{DeviceId} because {Reason}")]
    public static partial void DesfireEncodingRunDeviceUnavailable(this ILogger logger, Guid runId, string agentId, string deviceId, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Desfire encoding starting run {RunId} on device {AgentId}/{DeviceId}")]
    public static partial void DesfireEncodingRunStarting(this ILogger logger, Guid runId, string agentId, string deviceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Desfire encoding completed run {RunId} on device {AgentId}/{DeviceId} with card {CardUid}")]
    public static partial void DesfireEncodingRunCompleted(this ILogger logger, Guid runId, string agentId, string deviceId, string? cardUid);

    [LoggerMessage(Level = LogLevel.Information, Message = "Desfire encoding cancelled run {RunId} on device {AgentId}/{DeviceId}")]
    public static partial void DesfireEncodingRunCancelled(this ILogger logger, Guid runId, string agentId, string deviceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Desfire encoding failed run {RunId} on device {AgentId}/{DeviceId}")]
    public static partial void DesfireEncodingRunFailed(this ILogger logger, Guid runId, string agentId, string deviceId, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Desfire encoding queued hardware command {CommandId} for run {RunId} on device {AgentId}/{DeviceId} with capability {Capability}")]
    public static partial void DesfireEncodingHardwareCommandQueued(this ILogger logger, Guid runId, string agentId, string deviceId, string capability, Guid commandId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Desfire encoding notified agent for hardware command {CommandId} for run {RunId} on device {AgentId}/{DeviceId} with capability {Capability}")]
    public static partial void DesfireEncodingHardwareCommandNotified(this ILogger logger, Guid runId, string agentId, string deviceId, string capability, Guid commandId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Desfire encoding hardware command {CommandId} for run {RunId} on device {AgentId}/{DeviceId} with capability {Capability} completed with status {Status}")]
    public static partial void DesfireEncodingHardwareCommandCompleted(this ILogger logger, Guid runId, string agentId, string deviceId, string capability, Guid commandId, HardwareOperationStatus status);
}

public sealed record DesfireEncodingResult(BadgeJob? Run, IResult? Failure)
{
    public static DesfireEncodingResult NotFound => new(null, Results.NotFound());
}

public enum DesfireEncodingPhase
{
    WaitingForCard,
    Encoding,
    WaitingForRemoval
}

public sealed record CommandAuditItem(int Index, string Operation, bool Success, string StatusCode, string? CardUid);
