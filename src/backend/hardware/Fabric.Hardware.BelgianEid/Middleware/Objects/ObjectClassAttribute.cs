using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of ObjectClassAttribute.
    /// </summary>
    public class ObjectClassAttribute : UIntAttribute
    {
        public ObjectClassAttribute()
            : base((uint)CKA.CLASS) { }

        public ObjectClassAttribute(CK_ATTRIBUTE ckAttr)
            : base(ckAttr) { }

        public ObjectClassAttribute(CKO objectType)
            : base((uint)CKA.CLASS)
        {
            ObjectType = objectType;
        }

        public CKO ObjectType
        {
            get => (CKO)Value;
            internal set => Value = (uint)value;
        }

        public override string ToString()
        {
            return string.Format("[ObjectClassAttribute ObjectType={0}]", ObjectType);
        }

        protected override P11Attribute GetCkLoadedCopy()
        {
            return new ObjectClassAttribute(CK_ATTRIBUTE);
        }
    }
}
