using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Syndication;
using System.Xml.Serialization;
using System.Xml;

namespace FeedSync
{
	public class FeedSyncSyndicationFeed : SyndicationFeed
	{
		private Sharing sharing;

		public FeedSyncSyndicationFeed()
			: base()
		{
		}

		public FeedSyncSyndicationFeed(IEnumerable<SyndicationItem> items) 
			: base(items)
		{
		}

		protected FeedSyncSyndicationFeed(FeedSyncSyndicationFeed source, bool cloneItems)
			: base(source, cloneItems)
		{
			this.sharing = source.sharing.Clone();
		}

		public FeedSyncSyndicationFeed(string title, string description, Uri feedAlternateLink)
			: base(title, description, feedAlternateLink)
		{
		}

		public FeedSyncSyndicationFeed(string title, string description, Uri feedAlternateLink, IEnumerable<SyndicationItem> items)
			: base(title, description, feedAlternateLink, items)
		{
		}
    
		public FeedSyncSyndicationFeed(string title, string description, Uri feedAlternateLink, string id, DateTimeOffset lastUpdatedTime)
			: base(title, description, feedAlternateLink, id, lastUpdatedTime)
		{
		}

		public FeedSyncSyndicationFeed(string title, string description, Uri feedAlternateLink, string id, DateTimeOffset lastUpdatedTime, IEnumerable<SyndicationItem> items)
			: base(title, description, feedAlternateLink, id, lastUpdatedTime)
		{
		}
		
		public Sharing Sharing
		{
			get { return sharing; }
			set { sharing = value; }
		}

		protected override SyndicationItem CreateItem()
		{
			return new FeedSyncSyndicationItem();
		}

		public override SyndicationFeed Clone(bool cloneItems)
		{
			return new FeedSyncSyndicationFeed(this, cloneItems);
		}
		
		protected override bool TryParseElement(XmlReader reader, string version)
		{
			if (reader.LocalName == Schema.ElementNames.Sharing && reader.NamespaceURI == Schema.Namespace)
			{
				this.sharing = Sharing.Create(reader);

				return true;
			}
			
			return base.TryParseElement(reader, version);
		}

		protected override void WriteElementExtensions(XmlWriter writer, string version)
		{
			if (sharing != null)
			{
				((IXmlSerializable)this.sharing).WriteXml(writer);
			}

			base.WriteElementExtensions(writer, version);
		}
		
	}
}
