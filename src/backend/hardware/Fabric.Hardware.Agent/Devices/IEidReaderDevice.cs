using Fabric.Hardware.BelgianEid;
using Fabric.Hardware.Contracts.Inventory;

namespace Fabric.Hardware.Agent.Devices;

public interface IEidReaderDevice
{
    string DeviceId { get; }

    HardwareDeviceInventoryItem GetInventoryItem();

    Task<BelgianEidIdentity> ReadAsync(CancellationToken cancellationToken);

    Task<bool> VerifyPinAsync(string? pin, CancellationToken cancellationToken);

    Task WaitForRemovalAsync(CancellationToken cancellationToken);
}
