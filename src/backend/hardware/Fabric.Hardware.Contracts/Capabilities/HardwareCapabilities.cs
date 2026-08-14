namespace Fabric.Hardware.Contracts.Capabilities;

public static class HardwareCapabilities
{
    public const string QrScan = "qr.scan";
    public const string CardPresent = "card.present";
    public const string CardPrint = "card.print";
    public const string CardDrop = "card.drop";
    public const string CardDispense = "card.dispense";
    public const string CardCollect = "card.collect";
    public const string CardEject = "card.eject";
    public const string PassportScan = "passport.scan";
    public const string RfidRead = "rfid.read";
    public const string RfidEncode = "rfid.encode";
    public const string RfidApduExchange = "rfid.apdu.exchange";
    public const string LabelPrint = "label.print";
    public const string PrinterStatus = "printer.status";
}
