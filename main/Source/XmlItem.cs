using System;
using System.Xml;

namespace SimpleSharing
{
	[Serializable]
	public class XmlItem : IXmlItem
	{
		const string TitleXPath = "title";
		const string DescriptionXPath = "description";

		string id;
		DateTime lastUpdated = DateTime.Now;
		XmlElement payload;

		public XmlItem(string title, string description, XmlElement payload)
			: this(Guid.NewGuid().ToString(), title, description, DateTime.Now, payload)
		{
		}

		public XmlItem(string title, string description, DateTime timestamp, XmlElement payload)
			: this(Guid.NewGuid().ToString(), title, description, timestamp, payload)
		{
		}

		public XmlItem(string id, string title, string description, DateTime timestamp, XmlElement payload)
		{
			Guard.ArgumentNotNullOrEmptyString(id, "id");

			if (payload == null)
			{
				payload = new XmlDocument().CreateElement("payload");
			}

			SetNodeValue(payload, TitleXPath, title);
			SetNodeValue(payload, DescriptionXPath, description);

			this.id = id;
			Timestamp = timestamp;
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
			get { return GetNodeValue(payload, TitleXPath); }
			set { SetNodeValue(payload, TitleXPath, value); }
		}

		public string Description
		{
			get { return GetNodeValue(payload, DescriptionXPath); }
			set { SetNodeValue(payload, DescriptionXPath, value); }
		}

		public DateTime Timestamp
		{
			get { return lastUpdated; }
			set { lastUpdated = SimpleSharing.Timestamp.Normalize(value); }
		}

		public XmlElement Payload
		{
			get { return payload; }
			set { Guard.ArgumentNotNull(value, "Payload"); payload = value; }
		}

		#region XML Node Helpers

		private static void SetNodeValue(XmlElement element, string xPath, string value)
		{
			XmlNode node = element.SelectSingleNode(xPath);
			if (node == null)
			{
				if (value != null)
				{
					element.AppendChild(CreateNode(element, xPath, value));
				}
			}
			else
			{
				if (value == null)
				{
					element.RemoveChild(node);
				}
				else if (node.InnerText != value)
				{
					node.InnerText = value;
				}
			}
		}

		private string GetNodeValue(XmlElement element, string xPath)
		{
			XmlNode node = element.SelectSingleNode(xPath);
			if (node == null)
			{
				return null;
			}
			return node.InnerText;
		}

		private static XmlNode CreateNode(XmlElement element, string xPath, string value)
		{
			XmlNode node = element.OwnerDocument.CreateNode(XmlNodeType.Element, xPath, null);
			node.InnerText = value;
			return node;
		}

		#endregion

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
					obj1.Timestamp == obj2.Timestamp &&
					obj1.Payload.OuterXml == obj2.Payload.OuterXml;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return id.GetHashCode() ^ lastUpdated.GetHashCode() ^ payload.OuterXml.GetHashCode();
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
			return new XmlItem(id, GetNodeValue(payload, TitleXPath), GetNodeValue(payload, DescriptionXPath), lastUpdated,
				payload);
		}

		#endregion
	}
}