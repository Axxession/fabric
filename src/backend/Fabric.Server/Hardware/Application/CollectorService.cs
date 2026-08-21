using System.Text.Json.Nodes;
using Fabric.Hardware.Contracts;
using Fabric.Hardware.Contracts.Capabilities;
using Fabric.Hardware.Contracts.Commands;

namespace Fabric.Server.Hardware.Application;

public interface ICollectorService
{
    Task<CollectorCollectResponse> CollectAsync(HardwareDeviceRef device, CollectorCollectAction action, CancellationToken cancellationToken);
    Task<CollectorCardResponse> WaitForCardAsync(HardwareDeviceRef device, TimeSpan timeout, CancellationToken cancellationToken);
    Task<CollectorEjectResponse> EjectAsync(HardwareDeviceRef device, CancellationToken cancellationToken);
    Task<CollectorRemovalResponse> WaitForRemovalAsync(HardwareDeviceRef device, TimeSpan timeout, CancellationToken cancellationToken);
}

public sealed class CollectorService(
    HardwareCommandStore commandStore,
    HardwareAgentConnectionManager connectionManager) : ICollectorService
{
    private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds(30);

    public async Task<CollectorCollectResponse> CollectAsync(HardwareDeviceRef device, CollectorCollectAction action, CancellationToken cancellationToken)
    {
        var payload = new JsonObject { ["placeInCollectorStack"] = action == CollectorCollectAction.Collect };
        PostHardwareCommandResultRequest result = await ExecuteAsync(device, HardwareCapabilities.CardCollect, payload, DefaultCommandTimeout, cancellationToken);
        bool collected = result.Result?["collected"]?.GetValue<bool>() ?? false;
        return new CollectorCollectResponse(action, result.Status, collected, result.Error);
    }

    public async Task<CollectorCardResponse> WaitForCardAsync(HardwareDeviceRef device, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var payload = new JsonObject { ["timeoutSeconds"] = ToTimeoutSeconds(timeout) };
        PostHardwareCommandResultRequest result = await ExecuteAsync(device, HardwareCapabilities.CardPresent, payload, timeout, cancellationToken);
        string? cardNumber = result.Result?["cardNumber"]?.GetValue<string>();
        return new CollectorCardResponse(result.Status, cardNumber, result.Error);
    }

    public async Task<CollectorEjectResponse> EjectAsync(HardwareDeviceRef device, CancellationToken cancellationToken)
    {
        PostHardwareCommandResultRequest result = await ExecuteAsync(device, HardwareCapabilities.CardEject, payload: null, DefaultCommandTimeout, cancellationToken);
        bool ejected = result.Result?["ejected"]?.GetValue<bool>() ?? false;
        return new CollectorEjectResponse(result.Status, ejected, result.Error);
    }

    public async Task<CollectorRemovalResponse> WaitForRemovalAsync(HardwareDeviceRef device, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var payload = new JsonObject { ["timeoutSeconds"] = ToTimeoutSeconds(timeout) };
        PostHardwareCommandResultRequest result = await ExecuteAsync(device, HardwareCapabilities.CardWaitRemoval, payload, timeout, cancellationToken);
        bool removed = result.Result?["removed"]?.GetValue<bool>() ?? false;
        return new CollectorRemovalResponse(result.Status, removed, result.Error);
    }

    private async Task<PostHardwareCommandResultRequest> ExecuteAsync(HardwareDeviceRef device, string capability, JsonObject? payload, TimeSpan timeout, CancellationToken cancellationToken)
    {
        PendingHardwareCommand command = commandStore.Create(device.AgentId, device.DeviceId, capability, payload, timeout);
        connectionManager.NotifyCommandAvailable(device.AgentId, command.CommandId);
        return await commandStore.WaitForResultAsync(command, cancellationToken);
    }

    private static int ToTimeoutSeconds(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, null);

        double totalSeconds = Math.Ceiling(timeout.TotalSeconds);
        return totalSeconds > int.MaxValue ? int.MaxValue : (int)totalSeconds;
    }
}

public enum CollectorCollectAction
{
    Collect,
    Capture
}

public sealed record CollectorCollectResponse(
    CollectorCollectAction Action,
    HardwareOperationStatus Status,
    bool Collected,
    HardwareErrorResponse? Error);

public sealed record CollectorCardResponse(
    HardwareOperationStatus Status,
    string? CardNumber,
    HardwareErrorResponse? Error);

public sealed record CollectorEjectResponse(
    HardwareOperationStatus Status,
    bool Ejected,
    HardwareErrorResponse? Error);

public sealed record CollectorRemovalResponse(
    HardwareOperationStatus Status,
    bool Removed,
    HardwareErrorResponse? Error);
