#nullable disable
namespace Fabric.Hardware.BelgianEid.Middleware
{
    /// <summary>
    /// Description of Slot.
    /// </summary>
    public class Slot
    {
        Module m;

        public Module Module => m;

        uint slotId;

        public uint SlotId => slotId;

        public SlotInfo SlotInfo => new SlotInfo(m.P11Module.GetSlotInfo(slotId));

        public Token Token
        {
            get
            {
                Token localToken = null;

                if (SlotInfo.IsTokenPresent)
                {
                    localToken = new Token(this);
                }

                return localToken;
            }
        }

        public Slot(Module m, uint slotId)
        {
            this.m = m;
            this.slotId = slotId;
        }
    }
}
