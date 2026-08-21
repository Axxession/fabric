using Fabric.Hardware.Contracts;
using Fabric.Hardware.Contracts.Capabilities;
using Fabric.Hardware.Contracts.Commands;

namespace Fabric.Server.Hardware.Application;

public interface IDispenserService
{
    Task<DispenserCommandResponse> ExecuteAsync(HardwareDeviceRef device, DispenserAction action, CancellationToken cancellationToken);
}

public sealed class DispenserService(
    HardwareCommandStore commandStore,
    HardwareAgentConnectionManager connectionManager) : IDispenserService
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(30);

    public async Task<DispenserCommandResponse> ExecuteAsync(HardwareDeviceRef device, DispenserAction action, CancellationToken cancellationToken)
    {
        string capability = action switch
        {
            DispenserAction.FullDispense => HardwareCapabilities.CardDispense,
            DispenserAction.Prepare => HardwareCapabilities.CardPresent,
            DispenserAction.Drop => HardwareCapabilities.CardDrop,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

        PendingHardwareCommand command = commandStore.Create(device.AgentId, device.DeviceId, capability, payload: null, CommandTimeout);
        connectionManager.NotifyCommandAvailable(device.AgentId, command.CommandId);

        PostHardwareCommandResultRequest result = await commandStore.WaitForResultAsync(command, cancellationToken);
        string? cardNumber = result.Result?["cardNumber"]?.GetValue<string>();
        bool dropped = result.Result?["dropped"]?.GetValue<bool>() ?? false;

        return new DispenserCommandResponse(action, result.Status, cardNumber, dropped, result.Error);
    }
}

public enum DispenserAction
{
    FullDispense,
    Prepare,
    Drop
}

public sealed record DispenserCommandResponse(
    DispenserAction Action,
    HardwareOperationStatus Status,
    string? CardNumber,
    bool Dropped,
    HardwareErrorResponse? Error);
