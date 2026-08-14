namespace Fabric.Hardware.Contracts.Cards;

public sealed record PrintCardResponse(HardwareOperationStatus Status, HardwareErrorResponse? Error);
