using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    public class BooleanAttribute : P11Attribute
    {
        bool val_;

        public bool Value
        {
            get => val_;
            set
            {
                val_ = value;
                IsAssigned = true;
            }
        }

        internal BooleanAttribute(uint type)
            : base(type) { }

        internal BooleanAttribute(CKA type)
            : base((uint)type) { }

        internal BooleanAttribute(CK_ATTRIBUTE attr)
            : base(attr) { }

        public override byte[] Encode()
        {
            return new[] { (byte)(Value ? 1 : 0) };
        }

        public override void Decode(byte[] val)
        {
            Value = val[0] == 1;
        }

        public override string ToString()
        {
            return string.Format("[BooleanAttribute Value={0}]", val_);
        }

        protected override P11Attribute GetCkLoadedCopy()
        {
            return new BooleanAttribute(CK_ATTRIBUTE);
        }
    }
}
