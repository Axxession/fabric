namespace Fabric.Hardware.Agent.Options;

public static class EidReaderOptionsParser
{
    public static IReadOnlyList<EidReaderOptions> Parse(IConfiguration configuration)
    {
        IConfigurationSection eidReadersSection = configuration.GetSection($"{HardwareAgentOptions.SectionName}:EidReaders");
        if (!eidReadersSection.Exists())
            return [];

        List<EidReaderOptions> eidReaders = [];
        foreach (IConfigurationSection eidReaderSection in eidReadersSection.GetChildren())
        {
            string? type = eidReaderSection["$type"];
            if (string.Equals(type, "BelgianEidReader", StringComparison.OrdinalIgnoreCase))
            {
                eidReaders.Add(new BelgianEidReaderOptions
                {
                    DeviceId = eidReaderSection["deviceId"] ?? string.Empty,
                    Pkcs11ModulePath = eidReaderSection["pkcs11ModulePath"] ?? "beidpkcs11.dll",
                    ReadTimeoutMilliseconds = ParseInt(eidReaderSection["readTimeoutMilliseconds"], 250),
                    BypassPinCode = ParseBool(eidReaderSection["bypassPinCode"])
                });
                continue;
            }

            throw new InvalidOperationException($"Unsupported eID reader type '{type ?? "<null>"}'.");
        }

        return eidReaders;
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return int.TryParse(value, out int parsed)
            ? parsed
            : throw new InvalidOperationException($"Invalid integer '{value}'.");
    }

    private static bool ParseBool(string? value) =>
        !string.IsNullOrWhiteSpace(value) && bool.TryParse(value, out bool parsed)
            ? parsed
            : false;
}
