#nullable disable

using System.Text;
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of CharArrayAttribute.
    /// </summary>
    public class CharArrayAttribute : P11Attribute
    {
        char[] val;

        public char[] Value
        {
            get => val;
            set
            {
                val = value;
                IsAssigned = true;
            }
        }

        public CharArrayAttribute() { }

        public CharArrayAttribute(uint type)
            : base(type) { }

        public CharArrayAttribute(CKA type)
            : base((uint)type) { }

        public CharArrayAttribute(CK_ATTRIBUTE ckAttr)
            : base(ckAttr) { }

        public override byte[] Encode()
        {
            return Encoding.UTF8.GetBytes(new String(Value));
        }

        public override void Decode(byte[] val)
        {
            Value = Encoding.UTF8.GetString(val).ToCharArray();
        }

        public override string ToString()
        {
            return string.Format("[CharArrayAttribute Value={0}]", new String(val));
        }

        protected override P11Attribute GetCkLoadedCopy()
        {
            return new CharArrayAttribute(CK_ATTRIBUTE);
        }
    }
}
