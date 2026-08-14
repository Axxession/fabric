using Fabric.Hardware.Agent.Options;
using Fabric.Hardware.Contracts.Capabilities;
using Fabric.Hardware.Contracts.Inventory;

namespace Fabric.Hardware.Agent.Devices;

public abstract class BadgePrinterEncoderDeviceBase<TOptions>(TOptions options, ILogger logger)
    : PcscEncoderDeviceBase(options.Reader ?? string.Empty, options.Implementation)
    where TOptions : BadgePrinterEncoderOptions
{
    public override string DeviceId => options.DeviceId;

    protected TOptions Options => options;
    protected ILogger Logger => logger;

    protected abstract string Driver { get; }

    protected abstract bool IsTransportDetected();

    protected abstract Task LoadCardAsync(CancellationToken cancellationToken);

    protected abstract Task PrintCardAsync(byte[] image, CancellationToken cancellationToken);

    protected abstract Task EjectCardAsync(CancellationToken cancellationToken);

    protected virtual bool HasReader => !string.IsNullOrWhiteSpace(options.Reader);

    public override HardwareDeviceInventoryItem GetInventoryItem()
    {
        bool detected = HasReader ? ReaderExists() && IsTransportDetected() : IsTransportDetected();
        string connection = HasReader ? $"{options.PrinterName} | {options.Reader}" : options.PrinterName;
        string[] capabilities = HasReader
            ? [HardwareCapabilities.CardPresent, HardwareCapabilities.RfidApduExchange, HardwareCapabilities.CardPrint, HardwareCapabilities.CardEject]
            : [HardwareCapabilities.CardPresent, HardwareCapabilities.CardPrint, HardwareCapabilities.CardEject];
        return new HardwareDeviceInventoryItem(
            options.DeviceId,
            "encoder",
            Driver,
            capabilities,
            detected ? "online" : "offline",
            new HardwareDeviceDiagnostics(connection, Configured: !string.IsNullOrWhiteSpace(options.PrinterName), Detected: detected, Platform: Environment.OSVersion.Platform.ToString()));
    }

    public override async Task WaitForCardPresentAsync(CancellationToken cancellationToken)
    {
        if (HasReader)
            EnsureReaderAvailable();

        await LoadCardAsync(cancellationToken);

        if (!HasReader)
            return;

        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureReaderAvailable();

            if (IsCardPresent())
            {
                EnsureSession();
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
    }

    public override Task<byte[]> ExchangeApduAsync(byte[] command, CancellationToken cancellationToken)
    {
        if (!HasReader)
            throw new InvalidOperationException("Configured encoder does not support APDU exchange because no PCSC reader is configured.");

        return base.ExchangeApduAsync(command, cancellationToken);
    }

    public override Task PrintAsync(byte[] image, CancellationToken cancellationToken)
    {
        if (HasReader)
            EnsureReaderAvailable();

        return PrintCardAsync(image, cancellationToken);
    }

    public override async Task WaitForCardRemovalAsync(CancellationToken cancellationToken)
    {
        await EjectCardAsync(cancellationToken);

        if (!HasReader)
        {
            DisposeSession();
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!ReaderExists() || !IsCardPresent())
            {
                DisposeSession();
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }

    private void EnsureReaderAvailable()
    {
        if (!ReaderExists())
            throw new InvalidOperationException("Configured PCSC reader is not available.");
    }
}
