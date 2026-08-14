using System.Text.Json.Nodes;
using Fabric.Hardware.Contracts;
using Fabric.Hardware.Contracts.Capabilities;
using Fabric.Hardware.Contracts.Cards;
using Fabric.Hardware.Contracts.Commands;

namespace Fabric.Server.Hardware.Application;

public interface ICardPrinter
{
    Task<PrintCardResponse> PrintAsync(HardwareDeviceRef device, PrintCardRequest request, Guid ownerId, CancellationToken cancellationToken);
}

public sealed class CardPrinter(
    HardwareCommandStore commandStore,
    HardwareAgentConnectionManager connectionManager) : ICardPrinter
{
    private static readonly TimeSpan PrintTimeout = TimeSpan.FromMinutes(2);

    public async Task<PrintCardResponse> PrintAsync(HardwareDeviceRef device, PrintCardRequest request, Guid ownerId, CancellationToken cancellationToken)
    {
        var payload = new JsonObject
        {
            ["frontImageBase64"] = request.FrontImageBase64
        };

        PendingHardwareCommand command = commandStore.Create(device.AgentId, device.DeviceId, HardwareCapabilities.CardPrint, payload, PrintTimeout, ownerId);
        connectionManager.NotifyCommandAvailable(device.AgentId, command.CommandId);

        PostHardwareCommandResultRequest result = await commandStore.WaitForResultAsync(command, cancellationToken);
        return new PrintCardResponse(result.Status, result.Error);
    }
}
