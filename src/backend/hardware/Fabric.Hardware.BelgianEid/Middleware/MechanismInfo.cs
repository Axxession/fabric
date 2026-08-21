using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware
{
    public class MechanismInfo
    {
        CK_MECHANISM_INFO mi;

        internal MechanismInfo(CK_MECHANISM_INFO mi)
        {
            this.mi = mi;
        }

        public uint MaxKeySize => mi.ulMaxKeySize;

        public uint MinKeySize => mi.ulMinKeySize;

        public bool HW => (mi.flags & 0x00000001) > 0;

        public bool Encrypt => (mi.flags & 0x00000100) > 0;

        public bool Decrypt => (mi.flags & 0x00000200) > 0;

        public bool Digest => (mi.flags & 0x00000400) > 0;

        public bool Sign => (mi.flags & 0x00000800) > 0;

        public bool SignRecover => (mi.flags & 0x00001000) > 0;

        public bool Verify => (mi.flags & 0x00002000) > 0;

        public bool VerifyRecover => (mi.flags & 0x00004000) > 0;

        public bool Generate => (mi.flags & 0x00008000) > 0;

        public bool GenerateKeyPair => (mi.flags & 0x00010000) > 0;

        public bool Wrap => (mi.flags & 0x00020000) > 0;

        public bool Unwrap => (mi.flags & 0x00040000) > 0;

        public bool Derive => (mi.flags & 0x00080000) > 0;

        public bool Extension => (mi.flags & 0x80000000) > 0;
    }
}
