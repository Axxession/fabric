#nullable disable
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of Key.
    /// </summary>
    public abstract class Key : Storage
    {
        KeyTypeAttribute keyType = new();

        public KeyTypeAttribute KeyType => keyType;

        ByteArrayAttribute id = new(CKA.ID);

        public ByteArrayAttribute Id => id;

        DateAttribute startDate = new((uint)CKA.START_DATE);

        public DateAttribute StartDate => startDate;

        DateAttribute endDate = new((uint)CKA.END_DATE);

        public DateAttribute EndDate => endDate;

        BooleanAttribute derive = new(CKA.DERIVE);

        public BooleanAttribute Derive => derive;

        BooleanAttribute local = new(CKA.LOCAL);

        public BooleanAttribute Local => local;

        MechanismTypeAttribute keyGenMechanism = new(CKA.KEY_GEN_MECHANISM);

        public MechanismTypeAttribute KeyGenMechanism => keyGenMechanism;

        //TODO: CKA_ALLOWED_MECHANISMS

        public Key() { }

        public Key(Session session, uint hObj)
            : base(session, hObj) { }

        public static new P11Object GetInstance(Session session, uint hObj)
        {
            return null;
        }

        public override void ReadAttributes(Session session)
        {
            base.ReadAttributes(session);

            keyType = ReadAttribute(session, HObj, new KeyTypeAttribute());

            id = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.ID));

            startDate = ReadAttribute(session, HObj, new DateAttribute((uint)CKA.START_DATE));

            endDate = ReadAttribute(session, HObj, new DateAttribute((uint)CKA.END_DATE));

            derive = ReadAttribute(session, HObj, new BooleanAttribute(CKA.DERIVE));

            local = ReadAttribute(session, HObj, new BooleanAttribute(CKA.LOCAL));

            keyGenMechanism = ReadAttribute(session, HObj, new MechanismTypeAttribute(CKA.KEY_GEN_MECHANISM));
        }

        public override string ToString()
        {
            // This method returns the best value.
            if (Label.Value != null)
            {
                return base.ToString();
            }

            if (id.Value != null)
            {
                // Not bad, but could be better.
                return GetType().FullName + " " + id;
            }

            // Default handler.
            return base.ToString();
        }
    }
}
