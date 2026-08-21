using Fabric.Hardware.Contracts;
using Fabric.Hardware.Contracts.Capabilities;
using Fabric.Hardware.Contracts.Commands;

namespace Fabric.Server.Hardware.Application;

public interface IRfidReaderService
{
    Task<RfidReadResponse> ReadAsync(HardwareDeviceRef device, TimeSpan timeout, CancellationToken cancellationToken);
}

public sealed class RfidReaderService(
    HardwareCommandStore commandStore,
    HardwareAgentConnectionManager connectionManager) : IRfidReaderService
{
    public async Task<RfidReadResponse> ReadAsync(HardwareDeviceRef device, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, null);

        PendingHardwareCommand command = commandStore.Create(device.AgentId, device.DeviceId, HardwareCapabilities.RfidRead, payload: null, timeout);
        connectionManager.NotifyCommandAvailable(device.AgentId, command.CommandId);

        PostHardwareCommandResultRequest result = await commandStore.WaitForResultAsync(command, cancellationToken);
        string? cardNumber = result.Result?["cardNumber"]?.GetValue<string>();

        return new RfidReadResponse(result.Status, cardNumber, result.Error);
    }
}

public sealed record RfidReadResponse(
    HardwareOperationStatus Status,
    string? CardNumber,
    HardwareErrorResponse? Error);
