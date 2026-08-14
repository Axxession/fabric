namespace Fabric.Hardware.Agent.Options;

public static class EncoderOptionsParser
{
    public static IReadOnlyList<EncoderOptions> Parse(IConfiguration configuration)
    {
        IConfigurationSection encodersSection = configuration.GetSection($"{HardwareAgentOptions.SectionName}:Encoders");
        if (!encodersSection.Exists())
            return [];

        List<EncoderOptions> encoders = [];

        foreach (IConfigurationSection encoderSection in encodersSection.GetChildren())
        {
            string? type = encoderSection["$type"];
            if (string.Equals(type, "HumanAssistedEncoder", StringComparison.OrdinalIgnoreCase))
            {
                encoders.Add(new HumanAssistedEncoderOptions
                {
                    DeviceId = encoderSection["deviceId"] ?? string.Empty,
                    Reader = encoderSection["reader"] ?? string.Empty,
                    Implementation = ParseImplementation(encoderSection["implementation"])
                });
                continue;
            }

            if (string.Equals(type, "DispenserEncoder", StringComparison.OrdinalIgnoreCase))
            {
                encoders.Add(new DispenserEncoderOptions
                {
                    DeviceId = encoderSection["deviceId"] ?? string.Empty,
                    ComPort = encoderSection["comPort"] ?? string.Empty,
                    Reader = encoderSection["reader"] ?? string.Empty,
                    Implementation = ParseImplementation(encoderSection["implementation"]),
                    ResponseTimeout = ParseTimeSpan(encoderSection["responseTimeout"], TimeSpan.FromSeconds(5))
                });
                continue;
            }

            if (string.Equals(type, "EvolisEncoder", StringComparison.OrdinalIgnoreCase))
            {
                encoders.Add(new EvolisEncoderOptions
                {
                    DeviceId = encoderSection["deviceId"] ?? string.Empty,
                    PrinterName = encoderSection["printerName"] ?? string.Empty,
                    Reader = encoderSection["reader"] ?? string.Empty,
                    Implementation = ParseImplementation(encoderSection["implementation"]),
                    Station = encoderSection["station"] ?? "Contactless",
                    InputHopper = encoderSection["inputHopper"] ?? "0",
                    Verbose = ParseBool(encoderSection["verbose"])
                });
                continue;
            }

            if (string.Equals(type, "FargoEncoder", StringComparison.OrdinalIgnoreCase))
            {
                encoders.Add(new FargoEncoderOptions
                {
                    DeviceId = encoderSection["deviceId"] ?? string.Empty,
                    PrinterName = encoderSection["printerName"] ?? string.Empty,
                    Reader = encoderSection["reader"] ?? string.Empty,
                    Implementation = ParseImplementation(encoderSection["implementation"]),
                    Station = encoderSection["station"] ?? "Contactless",
                    InputHopper = encoderSection["inputHopper"] ?? "0",
                    Verbose = ParseBool(encoderSection["verbose"])
                });
                continue;
            }

            throw new InvalidOperationException($"Unsupported encoder type '{type ?? "<null>"}'.");
        }

        return encoders;
    }

    private static PcscEncoderImplementation ParseImplementation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return PcscEncoderImplementation.Iso;

        return Enum.TryParse<PcscEncoderImplementation>(value, ignoreCase: true, out PcscEncoderImplementation implementation)
            ? implementation
            : throw new InvalidOperationException($"Unsupported encoder implementation '{value}'. Expected Iso or Native.");
    }

    private static TimeSpan ParseTimeSpan(string? value, TimeSpan defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return TimeSpan.TryParse(value, out TimeSpan parsed)
            ? parsed
            : throw new InvalidOperationException($"Invalid time span '{value}'.");
    }

    private static bool ParseBool(string? value) =>
        !string.IsNullOrWhiteSpace(value) && bool.TryParse(value, out bool parsed)
            ? parsed
            : false;
}
