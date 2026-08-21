using System.Text;
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    public class Data : Storage
    {
        protected CharArrayAttribute application = new(CKA.APPLICATION);
        protected ByteArrayAttribute objectID = new(CKA.OBJECT_ID);
        protected ByteArrayAttribute value_ = new(CKA.VALUE);

        public CharArrayAttribute Application => application;

        public ByteArrayAttribute ObjectID => objectID;

        public ByteArrayAttribute Value => value_;

        public static new P11Object GetInstance(Session session, uint hObj)
        {
            return new Data(session, hObj);
        }

        public Data(Session session, uint hObj)
            : base(session, hObj) { }

        public Data()
        {
            Class.ObjectType = CKO.DATA;
        }

        public override void ReadAttributes(Session session)
        {
            base.ReadAttributes(session);

            application = ReadAttribute(session, HObj, new CharArrayAttribute(CKA.APPLICATION));
            objectID = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.OBJECT_ID));
            value_ = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.VALUE));
        }

        public override string ToString()
        {
            if (application.Value != null)
                return "Data object: " + new string(application.Value);
            if (objectID.Value != null)
                return "Data object: " + Encoding.ASCII.GetString(objectID.Value);
            return "Data object: " + base.ToString();
        }
    }
}
