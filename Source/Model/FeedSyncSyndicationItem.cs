using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Syndication;
using System.Xml.Serialization;
using System.Xml;

namespace FeedSync
{
	public class FeedSyncSyndicationItem : SyndicationItem
	{
		private Sync sync;

		public FeedSyncSyndicationItem()
			: base()
		{
		}

		protected FeedSyncSyndicationItem(FeedSyncSyndicationItem source)
			: base(source)
		{
			this.sync = source.sync.Clone();
		}

		public FeedSyncSyndicationItem(string title, string content, Uri itemAlternateLink) 
			: base(title, content, itemAlternateLink)
		{
		}

		public FeedSyncSyndicationItem(string title, SyndicationContent content, Uri itemAlternateLink, string id, DateTimeOffset lastUpdatedTime)
			 : base(title, content, itemAlternateLink, id, lastUpdatedTime)
		{
		}

		public FeedSyncSyndicationItem(string title, string content, Uri itemAlternateLink, string id, DateTimeOffset lastUpdatedTime)
			: base(title, content, itemAlternateLink, id, lastUpdatedTime)
		{
		}
		
		public Sync Sync
		{
			get { return sync; }
			set { sync = value; }
		}

		protected override bool TryParseElement(XmlReader reader, string version)
		{
			if (reader.LocalName == Schema.ElementNames.Sync && reader.NamespaceURI == Schema.Namespace)
			{
				this.sync = Sync.Create(reader);
				return true;
			}

			return base.TryParseElement(reader, version);
		}

		public override SyndicationItem Clone()
		{
			return new FeedSyncSyndicationItem(this);
		}

		protected override void WriteElementExtensions(XmlWriter writer, string version)
		{
			if (sync != null)
			{
				((IXmlSerializable)sync).WriteXml(writer);
			}

			base.WriteElementExtensions(writer, version);
		}
	
	}
}
