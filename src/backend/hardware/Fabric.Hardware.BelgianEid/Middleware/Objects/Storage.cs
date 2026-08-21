#nullable disable
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of Storage.
    /// </summary>
    public class Storage : P11Object
    {
        protected BooleanAttribute token_ = new(CKA.TOKEN);
        protected BooleanAttribute private_ = new(CKA.PRIVATE);
        protected BooleanAttribute modifiable_ = new(CKA.MODIFIABLE);
        protected CharArrayAttribute label_ = new(CKA.LABEL);

        public BooleanAttribute Token => token_;

        public BooleanAttribute Private => private_;

        public BooleanAttribute Modifiable => modifiable_;

        public CharArrayAttribute Label => label_;

        public Storage(Session session, uint hObj)
            : base(session, hObj) { }

        public Storage() { }

        public override void ReadAttributes(Session session)
        {
            base.ReadAttributes(session);

            token_ = ReadAttribute(session, HObj, new BooleanAttribute(CKA.TOKEN));

            private_ = ReadAttribute(session, HObj, new BooleanAttribute(CKA.PRIVATE));

            modifiable_ = ReadAttribute(session, HObj, new BooleanAttribute(CKA.MODIFIABLE));

            label_ = ReadAttribute(session, HObj, new CharArrayAttribute(CKA.LABEL));
        }

        public override string ToString()
        {
            if (label_.Value != null)
                return new string(label_.Value);
            return base.ToString();
        }
    }
}
