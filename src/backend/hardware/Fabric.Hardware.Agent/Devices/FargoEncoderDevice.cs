using Fargo.PrinterSDK;
using Fabric.Hardware.Agent.Options;

namespace Fabric.Hardware.Agent.Devices;

public sealed class FargoEncoderDevice(FargoEncoderOptions options, ILogger<FargoEncoderDevice> logger)
    : BadgePrinterEncoderDeviceBase<FargoEncoderOptions>(options, logger)
{
    private readonly StaThreadInvoker _staThread = new();

    protected override string Driver => "fargo-printer";

    protected override bool IsTransportDetected()
    {
        try
        {
            PrinterInfo printerInfo = new(Options.PrinterName);
            _ = printerInfo.CurrentActivity();
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected override async Task LoadCardAsync(CancellationToken cancellationToken)
    {
        Movement movement = new(Options.PrinterName);
        Station station = ParseStation(Options.Station);
        movement.MoveTo(station, ParseInputHopper(Options.InputHopper));

        if (!await WaitForDockAsync(station, cancellationToken))
            throw new InvalidOperationException($"Could not dock Fargo card at {station}.");
    }

    protected override async Task PrintCardAsync(byte[] image, CancellationToken cancellationToken)
    {
        using PrintJob printJob = new(Options.PrinterName);
        if (!printJob.AddPrintImageElement(image, 0, 0, 0, 0))
            throw new InvalidOperationException("Failed to add Fargo background image to print job.");

        bool result = await _staThread.InvokeAsync(() => printJob.DoPrint($"Printing job {Guid.NewGuid()}"));
        if (!result)
            throw new InvalidOperationException("Failed to submit Fargo print job.");

        CurrentActivity activity = printJob.FinishDoc();
        if (activity == CurrentActivity.CurrentActivityException)
            throw new InvalidOperationException("Failed to finalize Fargo print job.");
    }

    protected override async Task EjectCardAsync(CancellationToken cancellationToken)
    {
        Movement movement = new(Options.PrinterName);
        movement.MoveTo(Station.Eject, 0);

        if (!await PollForReadyAsync(cancellationToken))
            throw new InvalidOperationException("Could not eject Fargo card.");
    }

    public override void Dispose()
    {
        _staThread.Dispose();
        base.Dispose();
    }

    private async Task<bool> WaitForDockAsync(Station station, CancellationToken cancellationToken)
    {
        PrinterInfo printerInfo = new(Options.PrinterName);
        int tries = 100;

        if (station == Station.Magnetic)
            return true;

        if (Options.PrinterName.Contains("DTC", StringComparison.OrdinalIgnoreCase))
        {
            while (tries-- > 0 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                CurrentActivity currentActivity = printerInfo.CurrentActivity();
                if (currentActivity == CurrentActivity.CurrentActivityException)
                    return false;

                if (currentActivity is CurrentActivity.CurrentActivityEncodeContact or CurrentActivity.CurrentActivityEncodeContactless)
                    return true;
            }

            return false;
        }

        if (Options.PrinterName.Contains("HDP", StringComparison.OrdinalIgnoreCase))
        {
            while (tries-- > 0 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                StationStatus stationStatus = printerInfo.StationStatus(station);
                CurrentActivity currentActivity = printerInfo.CurrentActivity();
                if (currentActivity == CurrentActivity.CurrentActivityException)
                    return false;

                if (stationStatus == StationStatus.CardPresent)
                    return true;
            }

            return false;
        }

        throw new InvalidOperationException($"Unknown Fargo printer model '{Options.PrinterName}'. Expected DTC or HDP family.");
    }

    private async Task<bool> PollForReadyAsync(CancellationToken cancellationToken)
    {
        PrinterInfo printerInfo = new(Options.PrinterName);
        int tries = 100;
        while (tries-- > 0 && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            if (printerInfo.CurrentActivity() == CurrentActivity.CurrentActivityReady)
                return true;
        }

        return false;
    }

    private static Station ParseStation(string station)
    {
        if (!Enum.TryParse(station, ignoreCase: true, out Station parsed))
            throw new InvalidOperationException($"Unsupported Fargo station '{station}'.");

        return parsed;
    }

    private static int ParseInputHopper(string inputHopper) =>
        int.TryParse(inputHopper, out int hopper)
            ? hopper
            : throw new InvalidOperationException($"Invalid Fargo input hopper '{inputHopper}'.");
}
