using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware
{
    /// <summary>
    /// Description of Info.
    /// </summary>
    public class Info
    {
        protected Version cryptokiVersion_;

        public Version CryptokiVersion => cryptokiVersion_;

        protected String manufacturerID_;

        public string ManufacturerID => manufacturerID_;

        protected String libraryDescription_;

        public string LibraryDescription => libraryDescription_;

        protected Version libraryVersion_;

        public Version LibraryVersion => libraryVersion_;

        internal Info(CK_INFO ckInfo)
        {
            cryptokiVersion_ = new Version(ckInfo.cryptokiVersion);
            manufacturerID_ = P11Util.ConvertToUtf8String(ckInfo.manufacturerID);
            libraryDescription_ = P11Util.ConvertToUtf8String(ckInfo.libraryDescription);
            libraryVersion_ = new Version(ckInfo.libraryVersion);
        }

        public override string ToString()
        {
            return string.Format(
                "[Info CryptokiVersion={0} ManufacturerID={1} LibraryDescription={2} LibraryVersion={3}]",
                cryptokiVersion_,
                manufacturerID_,
                libraryDescription_,
                libraryVersion_
            );
        }
    }
}
