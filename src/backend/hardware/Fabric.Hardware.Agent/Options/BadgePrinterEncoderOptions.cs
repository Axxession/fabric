namespace Fabric.Hardware.Agent.Options;

public abstract class BadgePrinterEncoderOptions : EncoderOptions
{
    public required string PrinterName { get; init; }

    public string? Reader { get; init; }

    public PcscEncoderImplementation Implementation { get; init; } = PcscEncoderImplementation.Iso;

    public string Station { get; init; } = "Contactless";

    public string InputHopper { get; init; } = "0";

    public bool Verbose { get; init; }
}
