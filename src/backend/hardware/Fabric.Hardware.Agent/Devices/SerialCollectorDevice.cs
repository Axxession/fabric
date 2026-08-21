using Fabric.Hardware.Agent.Options;
using Fabric.Hardware.Collector;
using Fabric.Hardware.Contracts.Capabilities;
using Fabric.Hardware.Contracts.Inventory;
using CardCollector = Fabric.Hardware.Collector.Collector;

namespace Fabric.Hardware.Agent.Devices;

public sealed class SerialCollectorDevice(CollectorDeviceOptions options, ILogger<CardCollector> collectorLogger, ILogger<CollectorComPort> collectorComPortLogger, ILogger<SerialCollectorDevice> logger) : ICollectorDevice, IDisposable
{
    private readonly object _gate = new();
    private CardCollector? _collector;
    private bool _disposed;

    public string DeviceId => options.DeviceId;

    public string RfidReaderDeviceId => options.RfidReaderDeviceId;

    public TimeSpan RfidReadTimeout => options.RfidReadTimeout;

    public TimeSpan RemovalTimeout => options.RemovalTimeout;

    public HardwareDeviceInventoryItem GetInventoryItem()
    {
        bool detected = TryEnsureOpen();
        return new HardwareDeviceInventoryItem(
            options.DeviceId,
            "card-collector",
            "serial",
            [HardwareCapabilities.CardPresent, HardwareCapabilities.CardCollect, HardwareCapabilities.CardEject, HardwareCapabilities.CardWaitRemoval],
            detected ? "online" : "offline",
            new HardwareDeviceDiagnostics(options.ComPort, Configured: !string.IsNullOrWhiteSpace(options.ComPort), Detected: detected, Platform: Environment.OSVersion.Platform.ToString()));
    }

    public async Task WaitForCardAtReaderAsync(CancellationToken cancellationToken)
    {
        CardCollector collector = EnsureOpen();
        await collector.WaitForCardAtReaderAsync(cancellationToken);
    }

    public async Task<bool> CollectAsync(bool placeInCollectorStack, CancellationToken cancellationToken)
    {
        CardCollector collector = EnsureOpen();
        return placeInCollectorStack
            ? await collector.CollectAsync(cancellationToken)
            : await collector.CaptureAsync(cancellationToken);
    }

    public async Task<bool> EjectAsync(CancellationToken cancellationToken)
    {
        CardCollector collector = EnsureOpen();
        return await collector.EjectAsync(cancellationToken);
    }

    public async Task WaitForCardRemovalAsync(CancellationToken cancellationToken)
    {
        CardCollector collector = EnsureOpen();
        await collector.WaitForCardRemovalAsync(cancellationToken);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            _collector?.Dispose();
            _collector = null;
        }
    }

    private bool TryEnsureOpen()
    {
        try
        {
            EnsureOpen();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.CollectorOpenFailed(options.DeviceId, options.ComPort, ex);
            return false;
        }
    }

    private CardCollector EnsureOpen()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_collector is not null)
                return _collector;

            _collector = new CardCollector(
                new CollectorSettings
                {
                    ComPort = options.ComPort,
                    MaxJamCount = options.MaxJamCount,
                    PollingInterval = options.PollingInterval,
                    AckTimeout = options.AckTimeout,
                    MechanicalAckTimeout = options.MechanicalAckTimeout
                },
                collectorLogger,
                collectorComPortLogger);
            logger.CollectorOpened(options.DeviceId, options.ComPort);
            return _collector;
        }
    }
}
