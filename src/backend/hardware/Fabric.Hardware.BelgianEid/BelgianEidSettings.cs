namespace Fabric.Hardware.BelgianEid;

public sealed class BelgianEidSettings
{
    public bool BypassPinCode { get; init; }

    public int ReadTimeoutMilliseconds { get; init; } = 250;

    public string Pkcs11ModulePath { get; init; } = "beidpkcs11.dll";
}
