#nullable disable
using System.Runtime.InteropServices;
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    public abstract class P11Attribute
    {
        bool isAssigned;
        MetaData metaData = new();

        public MetaData MetaData => metaData;

        protected CK_ATTRIBUTE attr;

        internal uint Type
        {
            get => attr.type;
            private set => attr.type = value;
        }

        internal CKA CKA => (CKA)attr.type;

        public bool IsAssigned
        {
            get => isAssigned;
            protected set => isAssigned = value;
        }

        protected void AssignValue(byte[] val)
        {
            attr.ulValueLen = (uint)val.Length;
            attr.pValue = Marshal.AllocHGlobal(val.Length);
            Marshal.Copy(val, 0, attr.pValue, val.Length);
        }

        protected void AssignNullValue()
        {
            attr.pValue = IntPtr.Zero;
            attr.ulValueLen = 0;
        }

        internal virtual CK_ATTRIBUTE CK_ATTRIBUTE
        {
            get
            {
                if (IsAssigned)
                    AssignValue(Encode());
                else
                    AssignNullValue();

                return attr;
            }
        }

        public abstract byte[] Encode();

        public abstract void Decode(byte[] val);

        internal P11Attribute(CK_ATTRIBUTE attr)
        {
            this.attr = attr;
            DecodeAttr();
        }

        internal P11Attribute()
        {
            attr = new CK_ATTRIBUTE();
        }

        internal P11Attribute(uint type)
        {
            Type = type;
        }

        private byte[] getAsBinary(IntPtr ptr, int size)
        {
            if (ptr == IntPtr.Zero)
                return null;
            if (size == 0)
                return new byte[0];

            byte[] val = new byte[size];
            Marshal.Copy(ptr, val, 0, size);
            return val;
        }

        protected virtual void DecodeAttr()
        {
            byte[] tmp = getAsBinary(attr.pValue, (int)attr.ulValueLen);
            if (tmp != null && tmp.Length > 0)
                Decode(tmp);
        }

        public P11Attribute Load(CK_ATTRIBUTE attr)
        {
            this.attr = attr;
            DecodeAttr();
            return this;
        }

        public P11Attribute Clone()
        {
            P11Attribute p11 = GetCkLoadedCopy();
            p11.metaData = metaData;
            p11.isAssigned = isAssigned;
            return p11;
        }

        protected abstract P11Attribute GetCkLoadedCopy();
    }
}
