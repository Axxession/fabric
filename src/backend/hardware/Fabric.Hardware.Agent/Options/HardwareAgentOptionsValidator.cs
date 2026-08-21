namespace Fabric.Hardware.Agent.Options;

public sealed class HardwareAgentOptionsValidator(IReadOnlyList<EncoderOptions> encoders, IReadOnlyList<EidReaderOptions> eidReaders) : IValidateOptions<HardwareAgentOptions>
{
    public ValidateOptionsResult Validate(string? name, HardwareAgentOptions options)
    {
        List<string> failures = [];

        if (options.HeartbeatInterval <= TimeSpan.Zero)
            failures.Add("Heartbeat interval must be positive.");

        if (options.InventoryInterval <= TimeSpan.Zero)
            failures.Add("Inventory interval must be positive.");

        if (options.QrReaders.Length == 0 && options.Dispensers.Length == 0 && options.Collectors.Length == 0 && options.RfidEas.Readers.Length == 0 && encoders.Count == 0 && eidReaders.Count == 0)
            failures.Add("At least one hardware device must be configured.");

        foreach (EncoderOptions encoder in encoders)
        {
            if (string.IsNullOrWhiteSpace(encoder.DeviceId))
                failures.Add("Encoder device id is required.");

            if (encoder is HumanAssistedEncoderOptions humanAssistedEncoder)
            {
                if (string.IsNullOrWhiteSpace(humanAssistedEncoder.Reader))
                    failures.Add($"Encoder {humanAssistedEncoder.DeviceId} reader is required.");
            }

            if (encoder is DispenserEncoderOptions dispenserEncoder)
            {
                if (string.IsNullOrWhiteSpace(dispenserEncoder.ComPort))
                    failures.Add($"Encoder {dispenserEncoder.DeviceId} COM port is required.");

                if (string.IsNullOrWhiteSpace(dispenserEncoder.Reader))
                    failures.Add($"Encoder {dispenserEncoder.DeviceId} reader is required.");

                if (dispenserEncoder.ResponseTimeout <= TimeSpan.Zero)
                    failures.Add($"Encoder {dispenserEncoder.DeviceId} response timeout must be positive.");
            }

            if (encoder is BadgePrinterEncoderOptions badgePrinterEncoder)
            {
                if (string.IsNullOrWhiteSpace(badgePrinterEncoder.PrinterName))
                    failures.Add($"Encoder {badgePrinterEncoder.DeviceId} printer name is required.");

                if (string.IsNullOrWhiteSpace(badgePrinterEncoder.Station))
                    failures.Add($"Encoder {badgePrinterEncoder.DeviceId} station is required.");
            }
        }

        string[] duplicateEncoderIds = encoders
            .Where(encoder => !string.IsNullOrWhiteSpace(encoder.DeviceId))
            .GroupBy(encoder => encoder.DeviceId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (string duplicateEncoderId in duplicateEncoderIds)
            failures.Add($"Encoder device id '{duplicateEncoderId}' is configured more than once.");

        foreach (EidReaderOptions eidReader in eidReaders)
        {
            if (string.IsNullOrWhiteSpace(eidReader.DeviceId))
                failures.Add("eID reader device id is required.");

            if (eidReader is BelgianEidReaderOptions belgianEidReader)
            {
                if (string.IsNullOrWhiteSpace(belgianEidReader.Pkcs11ModulePath))
                    failures.Add($"eID reader {belgianEidReader.DeviceId} PKCS#11 module path is required.");

                if (belgianEidReader.ReadTimeoutMilliseconds <= 0)
                    failures.Add($"eID reader {belgianEidReader.DeviceId} read timeout milliseconds must be greater than zero.");
            }
        }

        string[] duplicateEidReaderIds = eidReaders
            .Where(eidReader => !string.IsNullOrWhiteSpace(eidReader.DeviceId))
            .GroupBy(eidReader => eidReader.DeviceId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (string duplicateEidReaderId in duplicateEidReaderIds)
            failures.Add($"eID reader device id '{duplicateEidReaderId}' is configured more than once.");

        foreach (QrReaderDeviceOptions qrReader in options.QrReaders)
        {
            if (string.IsNullOrWhiteSpace(qrReader.DeviceId))
                failures.Add("QR reader device id is required.");

            if (string.IsNullOrWhiteSpace(qrReader.ComPort))
                failures.Add($"QR reader {qrReader.DeviceId} COM port is required.");
        }

        string[] duplicateDeviceIds = options.QrReaders
            .Where(qrReader => !string.IsNullOrWhiteSpace(qrReader.DeviceId))
            .GroupBy(qrReader => qrReader.DeviceId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (string duplicateDeviceId in duplicateDeviceIds)
            failures.Add($"QR reader device id '{duplicateDeviceId}' is configured more than once.");

        foreach (DispenserDeviceOptions dispenser in options.Dispensers)
        {
            if (string.IsNullOrWhiteSpace(dispenser.DeviceId))
                failures.Add("Dispenser device id is required.");

            if (string.IsNullOrWhiteSpace(dispenser.ComPort))
                failures.Add($"Dispenser {dispenser.DeviceId} COM port is required.");

            if (string.IsNullOrWhiteSpace(dispenser.RfidReaderDeviceId))
                failures.Add($"Dispenser {dispenser.DeviceId} RFID reader device id is required.");

            if (dispenser.ResponseTimeout <= TimeSpan.Zero)
                failures.Add($"Dispenser {dispenser.DeviceId} response timeout must be positive.");

            if (dispenser.RfidReadTimeout <= TimeSpan.Zero)
                failures.Add($"Dispenser {dispenser.DeviceId} RFID read timeout must be positive.");
        }

        string[] duplicateDispenserIds = options.Dispensers
            .Where(dispenser => !string.IsNullOrWhiteSpace(dispenser.DeviceId))
            .GroupBy(dispenser => dispenser.DeviceId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (string duplicateDispenserId in duplicateDispenserIds)
            failures.Add($"Dispenser device id '{duplicateDispenserId}' is configured more than once.");

        foreach (CollectorDeviceOptions collector in options.Collectors)
        {
            if (string.IsNullOrWhiteSpace(collector.DeviceId))
                failures.Add("Collector device id is required.");

            if (string.IsNullOrWhiteSpace(collector.ComPort))
                failures.Add($"Collector {collector.DeviceId} COM port is required.");

            if (string.IsNullOrWhiteSpace(collector.RfidReaderDeviceId))
                failures.Add($"Collector {collector.DeviceId} RFID reader device id is required.");

            if (collector.PollingInterval <= TimeSpan.Zero)
                failures.Add($"Collector {collector.DeviceId} polling interval must be positive.");

            if (collector.RfidReadTimeout <= TimeSpan.Zero)
                failures.Add($"Collector {collector.DeviceId} RFID read timeout must be positive.");

            if (collector.RemovalTimeout <= TimeSpan.Zero)
                failures.Add($"Collector {collector.DeviceId} removal timeout must be positive.");

            if (collector.AckTimeout <= TimeSpan.Zero)
                failures.Add($"Collector {collector.DeviceId} ACK timeout must be positive.");

            if (collector.MechanicalAckTimeout <= TimeSpan.Zero)
                failures.Add($"Collector {collector.DeviceId} mechanical ACK timeout must be positive.");

            if (collector.MaxJamCount < 1)
                failures.Add($"Collector {collector.DeviceId} max jam count must be greater than zero.");
        }

        string[] duplicateCollectorIds = options.Collectors
            .Where(collector => !string.IsNullOrWhiteSpace(collector.DeviceId))
            .GroupBy(collector => collector.DeviceId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (string duplicateCollectorId in duplicateCollectorIds)
            failures.Add($"Collector device id '{duplicateCollectorId}' is configured more than once.");

        if (options.RfidEas.PollingDelay <= TimeSpan.Zero)
            failures.Add("RFID EAS polling delay must be positive.");

        if (options.RfidEas.DelayAfterRead <= TimeSpan.Zero)
            failures.Add("RFID EAS delay after read must be positive.");

        foreach (RfidEasReaderOptions rfidReader in options.RfidEas.Readers)
        {
            if (string.IsNullOrWhiteSpace(rfidReader.DeviceId))
                failures.Add("RFID EAS reader device id is required.");

            if (rfidReader.ReaderId < 0)
                failures.Add($"RFID EAS reader {rfidReader.DeviceId} reader id must be zero or greater.");
        }

        string[] duplicateRfidReaderIds = options.RfidEas.Readers
            .Where(rfidReader => !string.IsNullOrWhiteSpace(rfidReader.DeviceId))
            .GroupBy(rfidReader => rfidReader.DeviceId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (string duplicateRfidReaderId in duplicateRfidReaderIds)
            failures.Add($"RFID EAS reader device id '{duplicateRfidReaderId}' is configured more than once.");

        string[] duplicateHardwareDeviceIds = options.QrReaders.Select(qrReader => qrReader.DeviceId)
            .Concat(options.Dispensers.Select(dispenser => dispenser.DeviceId))
            .Concat(options.Collectors.Select(collector => collector.DeviceId))
            .Concat(options.RfidEas.Readers.Select(rfidReader => rfidReader.DeviceId))
            .Concat(eidReaders.Select(eidReader => eidReader.DeviceId))
            .Concat(encoders.Select(encoder => encoder.DeviceId))
            .Where(deviceId => !string.IsNullOrWhiteSpace(deviceId))
            .GroupBy(deviceId => deviceId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (string duplicateHardwareDeviceId in duplicateHardwareDeviceIds)
            failures.Add($"Hardware device id '{duplicateHardwareDeviceId}' is configured more than once.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
