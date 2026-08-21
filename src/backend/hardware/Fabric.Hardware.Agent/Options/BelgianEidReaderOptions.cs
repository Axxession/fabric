namespace Fabric.Hardware.Agent.Options;

public sealed class BelgianEidReaderOptions : EidReaderOptions
{
    public string Pkcs11ModulePath { get; init; } = "beidpkcs11.dll";

    public int ReadTimeoutMilliseconds { get; init; } = 250;

    public bool BypassPinCode { get; init; }
}
