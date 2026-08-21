#nullable disable
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of ByteArrayAttribute.
    /// </summary>
    public class ByteArrayAttribute : P11Attribute
    {
        byte[] val;

        public byte[] Value
        {
            get => val;
            set
            {
                val = value;
                IsAssigned = true;
            }
        }

        public ByteArrayAttribute(uint type)
            : base(type) { }

        public ByteArrayAttribute(CKA type)
            : base((uint)type) { }

        public ByteArrayAttribute(CK_ATTRIBUTE ckAttr)
            : base(ckAttr) { }

        public override byte[] Encode()
        {
            return Value;
        }

        public override void Decode(byte[] val)
        {
            Value = val;
        }

        public override string ToString()
        {
            return string.Format("[ByteArrayAttribute Value={0}]", BitConverter.ToString(val).Replace('-', ' '));
        }

        protected override P11Attribute GetCkLoadedCopy()
        {
            return new ByteArrayAttribute(CK_ATTRIBUTE);
        }
    }
}
