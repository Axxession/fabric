using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware
{
    /// <summary>
    /// Description of SlotInfo.
    /// </summary>
    public class SlotInfo
    {
        CK_SLOT_INFO ckSlotInfo;

        public string SlotDescription => P11Util.ConvertToUtf8String(ckSlotInfo.slotDescription);

        public string ManufacturerID => P11Util.ConvertToUtf8String(ckSlotInfo.manufacturerID);

        public Version FirmwareVersion => new Version(ckSlotInfo.firmwareVersion);

        public Version HardwareVersion => new Version(ckSlotInfo.hardwareVersion);

        public bool IsTokenPresent => ((ckSlotInfo.flags & 1L) != 0L);

        public bool IsRemovableDevice => ((ckSlotInfo.flags & 0x2) != 0L);

        public bool IsHwSlot => ((ckSlotInfo.flags & 0x4) != 0L);

        internal SlotInfo(CK_SLOT_INFO ckSlotInfo)
        {
            this.ckSlotInfo = ckSlotInfo;
        }
    }
}
