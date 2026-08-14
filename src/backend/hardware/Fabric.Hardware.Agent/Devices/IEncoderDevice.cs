using Fabric.Hardware.Contracts.Inventory;

namespace Fabric.Hardware.Agent.Devices;

public interface IEncoderDevice
{
    string DeviceId { get; }

    HardwareDeviceInventoryItem GetInventoryItem();

    Task WaitForCardPresentAsync(CancellationToken cancellationToken);

    Task<byte[]> ExchangeApduAsync(byte[] command, CancellationToken cancellationToken);

    Task PrintAsync(byte[] image, CancellationToken cancellationToken);

    Task WaitForCardRemovalAsync(CancellationToken cancellationToken);
}
