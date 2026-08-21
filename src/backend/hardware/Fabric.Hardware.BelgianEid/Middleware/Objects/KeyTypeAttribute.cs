using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of KeyTypeAttribute.
    /// </summary>
    public class KeyTypeAttribute : UIntAttribute
    {
        public KeyTypeAttribute()
            : base((uint)CKA.KEY_TYPE) { }

        public KeyTypeAttribute(CKK keyType)
            : base((uint)CKA.KEY_TYPE)
        {
            KeyType = keyType;
        }

        public KeyTypeAttribute(CK_ATTRIBUTE ckAttr)
            : base(ckAttr) { }

        public CKK KeyType
        {
            get => (CKK)Value;
            set => Value = (uint)value;
        }

        protected override P11Attribute GetCkLoadedCopy()
        {
            return new KeyTypeAttribute(CK_ATTRIBUTE);
        }
    }
}
