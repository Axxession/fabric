using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware
{
    /// <summary>
    /// Description of TokenInfo.
    /// </summary>
    public class TokenInfo
    {
        CK_TOKEN_INFO paramCK_TOKEN_INFO;

        internal TokenInfo(CK_TOKEN_INFO paramCK_TOKEN_INFO)
        {
            this.paramCK_TOKEN_INFO = paramCK_TOKEN_INFO;
        }

        public string Label => P11Util.ConvertToUtf8String(paramCK_TOKEN_INFO.label);

        public string ManufacturerID => P11Util.ConvertToUtf8String(paramCK_TOKEN_INFO.manufacturerID);

        public string Model => P11Util.ConvertToUtf8String(paramCK_TOKEN_INFO.model);

        public string SerialNumber => P11Util.ConvertToUtf8String(paramCK_TOKEN_INFO.serialNumber);

        public long MaxSessionCount => paramCK_TOKEN_INFO.ulMaxSessionCount;

        public long SessionCount => paramCK_TOKEN_INFO.ulSessionCount;

        public long MaxRwSessionCount => paramCK_TOKEN_INFO.ulMaxRwSessionCount;

        public long RwSessionCount => paramCK_TOKEN_INFO.ulRwSessionCount;

        public long MaxPinLen => paramCK_TOKEN_INFO.ulMaxPinLen;

        public long MinPinLen => paramCK_TOKEN_INFO.ulMinPinLen;

        public long TotalPublicMemory => paramCK_TOKEN_INFO.ulTotalPublicMemory;

        public long FreePublicMemory => paramCK_TOKEN_INFO.ulFreePublicMemory;

        public long TotalPrivateMemory => paramCK_TOKEN_INFO.ulTotalPrivateMemory;

        public long FreePrivateMemory => paramCK_TOKEN_INFO.ulFreePrivateMemory;

        public Version HardwareVersion => new Version(paramCK_TOKEN_INFO.hardwareVersion);

        public Version FirmwareVersion => new Version(paramCK_TOKEN_INFO.firmwareVersion);

        protected DateTime time;

        public DateTime Time => P11Util.ConvertToDateTimeYYYYMMDDhhmmssxx(P11Util.ConvertToASCIIString(paramCK_TOKEN_INFO.utcTime));

        public bool Rng => ((paramCK_TOKEN_INFO.flags & 1L) != 0L);

        public bool WriteProtected => ((paramCK_TOKEN_INFO.flags & 0x2) != 0L);

        public bool LoginRequired => ((paramCK_TOKEN_INFO.flags & 0x4) != 0L);

        public bool UserPinInitialized => ((paramCK_TOKEN_INFO.flags & 0x8) != 0L);

        public bool RestoreKeyNotNeeded => ((paramCK_TOKEN_INFO.flags & 0x20) != 0L);

        public bool ClockOnToken => ((paramCK_TOKEN_INFO.flags & 0x40) != 0L);

        public bool ProtectedAuthenticationPath => ((paramCK_TOKEN_INFO.flags & 0x100) != 0L);

        public bool DualCryptoOperations => ((paramCK_TOKEN_INFO.flags & 0x200) != 0L);

        public bool TokenInitialized => ((paramCK_TOKEN_INFO.flags & 0x400) != 0L);

        public bool SecondaryAuthentication => ((paramCK_TOKEN_INFO.flags & 0x800) != 0L);

        public bool UserPinCountLow => ((paramCK_TOKEN_INFO.flags & 0x10000) != 0L);

        public bool UserPinFinalTry => ((paramCK_TOKEN_INFO.flags & 0x20000) != 0L);

        public bool UserPinLocked => ((paramCK_TOKEN_INFO.flags & 0x40000) != 0L);

        public bool UserPinToBeChanged => ((paramCK_TOKEN_INFO.flags & 0x80000) != 0L);

        public bool SoPinCountLow => ((paramCK_TOKEN_INFO.flags & 0x100000) != 0L);

        public bool SoPinFinalTry => ((paramCK_TOKEN_INFO.flags & 0x200000) != 0L);

        public bool SoPinLocked => ((paramCK_TOKEN_INFO.flags & 0x400000) != 0L);

        public bool SoPinToBeChanged => ((paramCK_TOKEN_INFO.flags & 0x800000) != 0L);
    }
}
