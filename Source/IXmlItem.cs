using System;
using System.Xml;

namespace SimpleSharing
{
	public interface IXmlItem : ICloneable<IXmlItem>, IEquatable<IXmlItem>
	{
		string Title { get; set; }
		string Description { get; set; }
		string Id { get; set; }
		DateTime Timestamp { get; set; }
		XmlElement Payload { get; set; }
	}
}
