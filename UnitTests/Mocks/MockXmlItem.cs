using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using SimpleSharing;

namespace SimpleSharing.Tests
{
	public class MockXmlItem : IXmlItem
	{
		string id = Guid.NewGuid().ToString();
		XmlElement payload;
		DateTime timestamp = SimpleSharing.Timestamp.Normalize(DateTime.Now);

		public MockXmlItem()
		{
			payload = new XmlDocument().CreateElement("payload");
			payload.InnerXml = "<foo>bar</foo>";
		}

		public MockXmlItem(string id) : this()
		{
			this.id = id;
		}

		public string Description
		{
			get { return "Description"; }
			set { }
		}

		public string Id
		{
			get { return id; }
			set { }
		}

		public System.Xml.XmlElement Payload
		{
			get { return payload; }
			set { }
		}

		public object Hash
		{
			get { return timestamp; }
			set { timestamp = (DateTime)value; }
		}

		public string Title
		{
			get { return "Title"; }
			set { }
		}

		public IXmlItem Clone()
		{
			MockXmlItem item = new MockXmlItem();
			item.payload = this.payload;
			item.timestamp = this.timestamp;
			item.id = this.id;

			return item;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public bool Equals(IXmlItem other)
		{
			return other != null &&
				other is MockXmlItem &&
				other.Id == this.id &&
				other.Hash == this.Hash &&
				other.Payload.OuterXml == this.payload.OuterXml;
		}
	}
}
