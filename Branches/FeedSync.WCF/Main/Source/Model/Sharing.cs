using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml;
using System.Globalization;

namespace FeedSync
{
	public class Sharing : IXmlSerializable, ICloneable<Sharing>
	{
		private string since;
		private string until;
		private DateTime? expires;

		public Sharing()
		{
		}

		public static Sharing Create(XmlReader reader)
		{
			Sharing sharing = new Sharing();
			((IXmlSerializable)sharing).ReadXml(reader);

			return sharing;
		}

		public DateTime? Expires
		{
			get { return expires; }
			set
			{
				if (value != null)
				{
					expires = Timestamp.Normalize(value.Value);
				}
				else
				{
					expires = value;
				}
			}
		}

		/// <summary>
		/// Typically, a date time in a normalized string form.
		/// </summary>
		public string Since
		{
			get { return since; }
			set { since = value; }
		}

		/// <summary>
		/// Typically, a date time in a normalized string form.
		/// </summary>
		public string Until
		{
			get { return until; }
			set { until = value; }
		}

		private List<Related> related = new List<Related>();

		public List<Related> Related
		{
			get { return related; }
			set { related = value; }
		}

		#region IXmlSerializable Members

		XmlSchema IXmlSerializable.GetSchema()
		{
			return null;
		}

		void IXmlSerializable.ReadXml(XmlReader reader)
		{

			if (reader.MoveToAttribute(Schema.AttributeNames.Since))
				this.since = reader.Value;
			if (reader.MoveToAttribute(Schema.AttributeNames.Until))
				this.until = reader.Value;
			if (reader.MoveToAttribute(Schema.AttributeNames.Expires))
				this.expires = DateTime.Parse(reader.Value);

			if (reader.NodeType == XmlNodeType.Attribute)
				reader.MoveToElement();

			if (!reader.IsEmptyElement)
			{
				while (reader.Read() &&
					!IsFeedSyncElement(reader, Schema.ElementNames.Sharing, XmlNodeType.EndElement))
				{
					if (IsFeedSyncElement(reader, Schema.ElementNames.Related, XmlNodeType.Element))
					{
						this.Related.Add(new Related(
							reader.GetAttribute(Schema.AttributeNames.Link),
							(RelatedType)Enum.Parse(
								typeof(RelatedType),
								CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
									reader.GetAttribute(Schema.AttributeNames.Type)),
									false),
							reader.GetAttribute(Schema.AttributeNames.Title)));
					}
				}
			}
		}

		void IXmlSerializable.WriteXml(XmlWriter writer)
		{
			// <sx:sharing>
			writer.WriteStartElement(Schema.DefaultPrefix, Schema.ElementNames.Sharing, Schema.Namespace);
			if (this.Since != null)
				writer.WriteAttributeString(Schema.AttributeNames.Since, this.Since);
			if (this.Until != null)
				writer.WriteAttributeString(Schema.AttributeNames.Until, this.Until);
			if (this.Expires != null)
				writer.WriteAttributeString(Schema.AttributeNames.Expires, Timestamp.ToString(this.Expires.Value));

			foreach (Related rel in this.Related)
			{
				WriteRelated(writer, rel);
			}

			// </sx:sharing>
			writer.WriteEndElement();
		}

		private void WriteRelated(XmlWriter writer, Related related)
		{
			// <sx:related>
			writer.WriteStartElement(Schema.DefaultPrefix, Schema.ElementNames.Related, Schema.Namespace);
			writer.WriteAttributeString(Schema.AttributeNames.Link, related.Link);
			if (related.Title != null)
				writer.WriteAttributeString(Schema.AttributeNames.Title, related.Title);
			writer.WriteAttributeString(Schema.AttributeNames.Type, related.Type.ToString().ToLower());
			writer.WriteEndElement();
		}

		private static bool IsFeedSyncElement(XmlReader reader, string elementName, XmlNodeType nodeType)
		{
			return reader.LocalName == elementName &&
				reader.NamespaceURI == Schema.Namespace &&
				reader.NodeType == nodeType;
		}

		#endregion



		#region ICloneable<Sharing> Members

		public Sharing Clone()
		{
			Sharing newSharing = new Sharing();
			newSharing.expires = expires;
			newSharing.since = since;
			newSharing.until = until;

			foreach (Related related in this.related)
			{
				newSharing.related.Add(related.Clone());
			}

			return newSharing;
		}

		#endregion

		#region ICloneable Members

		object ICloneable.Clone()
		{
			return Clone();
		}

		#endregion
	}
}
