using Fabric.Hardware.Contracts.Capabilities;
using Fabric.Server.Kiosk.Domain;

namespace Fabric.Server.Kiosk.Application;

public static class KioskDeviceCapabilities
{
    public static IReadOnlyList<string> GetRequiredCapabilities(KioskDeviceType type) => type switch
    {
        KioskDeviceType.QrReader => [HardwareCapabilities.QrScan],
        KioskDeviceType.RfidReader => [HardwareCapabilities.RfidRead],
        KioskDeviceType.Dispenser => [HardwareCapabilities.CardDispense],
        KioskDeviceType.Collector => [HardwareCapabilities.CardPresent, HardwareCapabilities.CardCollect, HardwareCapabilities.CardEject, HardwareCapabilities.CardWaitRemoval],
        KioskDeviceType.Encoder => [HardwareCapabilities.CardPresent, HardwareCapabilities.RfidApduExchange, HardwareCapabilities.CardEject],
        KioskDeviceType.EidReader => [HardwareCapabilities.EidRead],
        KioskDeviceType.PassportReader => [HardwareCapabilities.PassportScan],
        KioskDeviceType.LabelPrinter => [HardwareCapabilities.LabelPrint],
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static IReadOnlyList<string> GetCleanupCapabilities(KioskDeviceType type) => type switch
    {
        KioskDeviceType.Collector => [HardwareCapabilities.CardEject],
        _ => GetRequiredCapabilities(type)
    };
}
