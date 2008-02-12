using System;
using System.Xml;
using System.ServiceModel.Syndication;

namespace FeedSync
{
	public class XmlItem : ICloneable<XmlItem>, IEquatable<XmlItem>
	{
		private string id;
		private string description;
		private string title;
		private object tag;

		private SyndicationContent payload;

		public XmlItem(string title, string description, SyndicationContent payload, object tag)
			: this(Guid.NewGuid().ToString(), title, description, payload, tag)
		{
		}

		public XmlItem(string title, string description, SyndicationContent payload)
			: this(Guid.NewGuid().ToString(), title, description, payload)
		{
		}


		public XmlItem(string id, string title, string description, SyndicationContent payload)
			: this(id, title, description, payload, null)
		{
		}

		public XmlItem(string id, string title, string description, SyndicationContent payload, object tag)
		{
			Guard.ArgumentNotNullOrEmptyString(id, "id");

			this.id = id;
			this.title = title;
			this.description = description;
			this.tag = tag;
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

		public SyndicationContent Payload
		{
			get { return payload; }
			set
			{
				Guard.ArgumentNotNull(value, "Payload");
				payload = value;
			}
		}

		public object Tag
		{
			get { return tag; }
			set
			{
				Guard.ArgumentNotNull(value, "Tag");
				tag = value;
			}
		}

		#region Equality

		public bool Equals(XmlItem other)
		{
			return XmlItem.Equals(this, other);
		}

		public override bool Equals(object obj)
		{
			return XmlItem.Equals(this, obj as XmlItem);
		}

		public static bool Equals(XmlItem obj1, XmlItem obj2)
		{
			if (Object.ReferenceEquals(obj1, obj2))
				return true;
			if (!Object.Equals(null, obj1) && !Object.Equals(null, obj2))
			{
				return obj1.Id == obj2.Id &&
					obj1.Title == obj2.Title &&
					obj1.Description == obj2.Description &&
					obj1.Tag == obj2.Tag;

			}

			return false;
		}

		public override int GetHashCode()
		{
			string resultingPayload = id.ToString() +
				((title != null) ? title : "") +
				((description != null) ? description : "");

			return resultingPayload.GetHashCode() + payload.GetHashCode();
		}

		#endregion

		#region ICloneable Members

		object ICloneable.Clone()
		{
			return DoClone();
		}

		public XmlItem Clone()
		{
			return DoClone();
		}

		protected virtual XmlItem DoClone()
		{
			return new XmlItem(id, title, description, payload, tag);
		}

		#endregion
	}
}