using Fabric.Hardware.BelgianEid.Middleware.Objects;

namespace Fabric.Hardware.BelgianEid.Middleware
{
    /// <summary>
    /// Description of KeyPait.
    /// </summary>
    public class KeyPair
    {
        PublicKey pubKey;
        PrivateKey privKey;

        public PublicKey PublicKey => pubKey;

        public PrivateKey PrivateKey => privKey;

        public KeyPair(PublicKey publicKey, PrivateKey privateKey)
        {
            pubKey = publicKey;
            privKey = privateKey;
        }
    }
}
