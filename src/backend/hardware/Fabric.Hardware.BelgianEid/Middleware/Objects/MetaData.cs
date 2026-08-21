namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    public class MetaData
    {
        bool isPresent;

        public bool IsPresent
        {
            get => isPresent;
            internal set => isPresent = value;
        }

        bool isSensitive;

        public bool IsSensitive
        {
            get => isSensitive;
            internal set => isSensitive = value;
        }
    }
}
