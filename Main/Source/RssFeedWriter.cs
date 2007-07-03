using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;

namespace SimpleSharing
{
	public class RssFeedWriter : FeedWriter
	{
		public RssFeedWriter(XmlWriter writer) : base(writer) { }

		protected override void WriteStartFeed(Feed feed, XmlWriter writer)
		{
			writer.WriteStartElement("rss");
			writer.WriteAttributeString("version", "2.0");
			writer.WriteStartElement("channel");
			writer.WriteElementString("title", feed.Title);
			writer.WriteElementString("description", feed.Description);
			writer.WriteElementString("link", feed.Link);
		}

		protected override void WriteEndFeed(Feed feed, XmlWriter writer)
		{
			writer.WriteEndElement();
			writer.WriteEndElement();
		}

		protected override void WriteStartItem(Item item, XmlWriter writer)
		{
			writer.WriteStartElement("item");
			if (!item.Sync.Deleted)
			{
				writer.WriteElementString("title", item.XmlItem.Title);
				writer.WriteElementString("description", item.XmlItem.Description);
				writer.WriteNode(new XmlNodeReader(item.XmlItem.Payload), false);
			}
			else
			{
				writer.WriteElementString("title", String.Format(
					CultureInfo.CurrentCulture, 
					Properties.Resources.DeletedTitle, 
					item.Sync.LastUpdate.When.Value.ToShortDateString(), 
					item.Sync.LastUpdate.By));
			}
		}

		protected override void WriteEndItem(Item item, XmlWriter writer)
		{
			writer.WriteEndElement();
		}
	}
}
