using System.Text.Json.Nodes;
using Fabric.Hardware.Contracts;
using Fabric.Hardware.Contracts.Capabilities;
using Fabric.Hardware.Contracts.Commands;

namespace Fabric.Server.Hardware.Application;

public interface IEidReaderService
{
    Task<EidReadResponse> ReadAsync(HardwareDeviceRef device, CancellationToken cancellationToken);
    Task<EidVerifyPinResponse> VerifyPinAsync(HardwareDeviceRef device, string pin, CancellationToken cancellationToken);
    Task<EidWaitRemovalResponse> WaitForRemovalAsync(HardwareDeviceRef device, CancellationToken cancellationToken);
}

public sealed class EidReaderService(
    HardwareCommandStore commandStore,
    HardwareAgentConnectionManager connectionManager) : IEidReaderService
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(2);

    public async Task<EidReadResponse> ReadAsync(HardwareDeviceRef device, CancellationToken cancellationToken)
    {
        PostHardwareCommandResultRequest result = await ExecuteAsync(device, HardwareCapabilities.EidRead, payload: null, cancellationToken);
        DateOnly? expiryDate = null;
        if (DateOnly.TryParse(result.Result?["expiryDate"]?.GetValue<string>(), out DateOnly parsedExpiryDate))
            expiryDate = parsedExpiryDate;

        return new EidReadResponse(
            result.Status,
            result.Result?["firstName"]?.GetValue<string>(),
            result.Result?["lastName"]?.GetValue<string>(),
            result.Result?["nationalNumber"]?.GetValue<string>(),
            result.Result?["documentNumber"]?.GetValue<string>(),
            expiryDate,
            result.Result?["nationality"]?.GetValue<string>(),
            result.Result?["birthLocation"]?.GetValue<string>(),
            result.Result?["birthDateRaw"]?.GetValue<string>(),
            result.Error);
    }

    public async Task<EidVerifyPinResponse> VerifyPinAsync(HardwareDeviceRef device, string pin, CancellationToken cancellationToken)
    {
        var payload = new JsonObject { ["pin"] = pin };
        PostHardwareCommandResultRequest result = await ExecuteAsync(device, HardwareCapabilities.EidVerifyPin, payload, cancellationToken);
        bool validPin = result.Result?["validPin"]?.GetValue<bool>() ?? false;
        return new EidVerifyPinResponse(result.Status, validPin, result.Error);
    }

    public async Task<EidWaitRemovalResponse> WaitForRemovalAsync(HardwareDeviceRef device, CancellationToken cancellationToken)
    {
        PostHardwareCommandResultRequest result = await ExecuteAsync(device, HardwareCapabilities.EidWaitRemoval, payload: null, cancellationToken);
        bool removed = result.Result?["removed"]?.GetValue<bool>() ?? false;
        return new EidWaitRemovalResponse(result.Status, removed, result.Error);
    }

    private async Task<PostHardwareCommandResultRequest> ExecuteAsync(HardwareDeviceRef device, string capability, JsonObject? payload, CancellationToken cancellationToken)
    {
        PendingHardwareCommand command = commandStore.Create(device.AgentId, device.DeviceId, capability, payload, CommandTimeout);
        connectionManager.NotifyCommandAvailable(device.AgentId, command.CommandId);
        return await commandStore.WaitForResultAsync(command, cancellationToken);
    }
}

public sealed record EidReadResponse(
    HardwareOperationStatus Status,
    string? FirstName,
    string? LastName,
    string? NationalNumber,
    string? DocumentNumber,
    DateOnly? ExpiryDate,
    string? Nationality,
    string? BirthLocation,
    string? BirthDateRaw,
    HardwareErrorResponse? Error);

public sealed record EidVerifyPinResponse(
    HardwareOperationStatus Status,
    bool ValidPin,
    HardwareErrorResponse? Error);

public sealed record EidWaitRemovalResponse(
    HardwareOperationStatus Status,
    bool Removed,
    HardwareErrorResponse? Error);
