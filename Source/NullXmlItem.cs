using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SimpleSharing
{
	// TODO: review the need for this class.
	public class NullXmlItem : IXmlItem
	{
		string id;
		DateTime timestamp = DateTime.MinValue;
		XmlElement emptyPayload;

		public NullXmlItem(string id)
		{
			Guard.ArgumentNotNullOrEmptyString(id, "id");

			this.id = id;
			emptyPayload = new XmlDocument().CreateElement("payload");
		}

		public string Id
		{
			get { return id; }
			set { id = value; }
		}

		public DateTime Timestamp
		{
			get { return timestamp; }
			set { timestamp = value; }
		}

		public string Title
		{
			get { return String.Empty; }
			set { }
		}

		public string Description
		{
			get { return String.Empty;  }
			set { }
		}

		public XmlElement Payload
		{
			get { return emptyPayload; }
			set { }
		}

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
			return new NullXmlItem(id);
		}

		#endregion

		#region Equality

		public bool Equals(IXmlItem other)
		{
			return NullXmlItem.Equals(this, other as NullXmlItem);
		}

		public override bool Equals(object obj)
		{
			return NullXmlItem.Equals(this, obj as NullXmlItem);
		}

		public static bool Equals(NullXmlItem obj1, NullXmlItem obj2)
		{
			if (Object.ReferenceEquals(obj1, obj2)) return true;
			if (!Object.Equals(null, obj1) && !Object.Equals(null, obj2))
			{
				return obj1.id == obj2.id && 
					obj1.timestamp == obj2.timestamp;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return id.GetHashCode() ^ timestamp.GetHashCode();
		}

		#endregion
	}
}
