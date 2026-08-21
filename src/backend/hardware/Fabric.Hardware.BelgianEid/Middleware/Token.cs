using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware
{
    /// <summary>
    /// Description of Token.
    /// </summary>
    public class Token
    {
        protected Slot slot_;

        public Slot Slot => slot_;

        public Module Module => slot_.Module;

        public Token(Slot slot)
        {
            slot_ = slot;
        }

        public uint TokenId => slot_.SlotId;

        public TokenInfo TokenInfo => new TokenInfo(slot_.Module.P11Module.GetTokenInfo(slot_.SlotId));

        public CKM[] MechanismList => Module.P11Module.GetMechanismList(TokenId);

        public MechanismInfo GetMechanismInfo(CKM ckm)
        {
            return new MechanismInfo(Module.P11Module.GetMechanismInfo(TokenId, ckm));
        }

        public Session OpenSession(bool readOnly)
        {
            return new Session(this, slot_.Module.P11Module.OpenSession(slot_.SlotId, 0, readOnly));
        }

        public void InitToken(string pin, string label)
        {
            Module.P11Module.InitToken(slot_.SlotId, pin, label);
        }
    }
}
