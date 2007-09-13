using System;
using System.Xml;

namespace SimpleSharing
{
    [Serializable]
    public class XmlItem : IXmlItem
    {
        private string id;
        private string description;
        private string title;
        object hash = null;
        private XmlElement payload;

        public XmlItem(string title, string description, XmlElement payload)
            : this(Guid.NewGuid().ToString(), title, description, null, payload)
        {
        }

        public XmlItem(string title, string description, object hash, XmlElement payload)
            : this(Guid.NewGuid().ToString(), title, description, hash, payload)
        {
        }

        public XmlItem(string id, string title, string description, object hash, XmlElement payload)
        {
            Guard.ArgumentNotNullOrEmptyString(id, "id");

            if (payload == null)
            {
                payload = new XmlDocument().CreateElement("payload");
            }

            this.id = id;
            this.title = title;
            this.description = description;
            Hash = hash;
            this.payload = payload;
        }

        public string Id
        {
            get { return id; }
            set
            {
                Guard.ArgumentNotNullOrEmptyString(value, "Id");
                id = value;
            }
        }

        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        public object Hash
        {
            get { return hash; }
            set { hash = value; }
        }

        public XmlElement Payload
        {
            get { return payload; }
            set
            {
                Guard.ArgumentNotNull(value, "Payload");
                payload = value;
            }
        }

        #region Equality

        public bool Equals(IXmlItem other)
        {
            return XmlItem.Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            return XmlItem.Equals(this, obj as IXmlItem);
        }

        public static bool Equals(IXmlItem obj1, IXmlItem obj2)
        {
            if (Object.ReferenceEquals(obj1, obj2))
                return true;
            if (!Object.Equals(null, obj1) && !Object.Equals(null, obj2))
            {
                return obj1.Id == obj2.Id &&
                    obj1.Title == obj2.Title &&
                    obj1.Description == obj2.Description &&
                    ((obj1.Hash == null && obj2.Hash == null) || obj1.Hash.Equals(obj2.Hash)) &&
                    obj1.Payload.OuterXml == obj2.Payload.OuterXml;
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hash = id.GetHashCode();
            if (title != null)
                hash = hash ^ title.GetHashCode();
            if (description != null)
                hash = hash ^ description.GetHashCode();
            if (this.Hash != null)
                hash = hash ^ this.Hash.GetHashCode();
            hash = hash ^ payload.OuterXml.GetHashCode();

            return hash;
        }

        #endregion

        #region ICloneable Members

        object ICloneable.Clone()
        {
            return DoClone();
        }

        public IXmlItem Clone()
        {
            return DoClone();
        }

        protected virtual IXmlItem DoClone()
        {
            return new XmlItem(id, title, description, hash, payload);
        }

        #endregion
    }
}