using Fabric.Hardware.Agent.Options;
using Fabric.Hardware.BelgianEid;
using Fabric.Hardware.Contracts.Capabilities;
using Fabric.Hardware.Contracts.Inventory;

namespace Fabric.Hardware.Agent.Devices;

public sealed class BelgianEidReaderDevice(BelgianEidReaderOptions options, ILogger<BelgianEidReader> readerLogger, ILogger<BelgianEidReaderDevice> logger) : IEidReaderDevice
{
    private readonly Lazy<BelgianEidReader> _reader = new(() => new BelgianEidReader(
        new BelgianEidSettings
        {
            BypassPinCode = options.BypassPinCode,
            ReadTimeoutMilliseconds = options.ReadTimeoutMilliseconds,
            Pkcs11ModulePath = options.Pkcs11ModulePath
        },
        readerLogger));

    public string DeviceId => options.DeviceId;

    public HardwareDeviceInventoryItem GetInventoryItem()
    {
        bool detected = TryEnsureReader();
        return new HardwareDeviceInventoryItem(
            options.DeviceId,
            "eid-reader",
            "belgian-eid-pkcs11",
            [HardwareCapabilities.EidRead, HardwareCapabilities.EidVerifyPin, HardwareCapabilities.EidWaitRemoval],
            detected ? "online" : "offline",
            new HardwareDeviceDiagnostics(options.Pkcs11ModulePath, Configured: !string.IsNullOrWhiteSpace(options.Pkcs11ModulePath), Detected: detected, Platform: Environment.OSVersion.Platform.ToString()));
    }

    public async Task<BelgianEidIdentity> ReadAsync(CancellationToken cancellationToken) =>
        await _reader.Value.ReadAsync(cancellationToken);

    public async Task<bool> VerifyPinAsync(string? pin, CancellationToken cancellationToken) =>
        await _reader.Value.VerifyPinAsync(pin, cancellationToken);

    public async Task WaitForRemovalAsync(CancellationToken cancellationToken) =>
        await _reader.Value.WaitForRemovalAsync(cancellationToken);

    private bool TryEnsureReader()
    {
        try
        {
            _ = _reader.Value;
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or InvalidOperationException or TypeInitializationException)
        {
            logger.BelgianEidUnavailable(options.DeviceId, ex);
            return false;
        }
    }
}
