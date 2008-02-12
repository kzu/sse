using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Syndication;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace FeedSync
{
	public class FeedSyncSyndicationItem : SyndicationItem
	{
		private Sync sync;

		public FeedSyncSyndicationItem()
			: base()
		{
		}

		public FeedSyncSyndicationItem(Sync sync)
			: base()
		{
			Guard.ArgumentNotNull(sync, "sync");

			this.sync = sync;
			this.Id = sync.Id;
		}

		public FeedSyncSyndicationItem(FeedSyncSyndicationItem source)
			: base(source)
		{
			this.sync = source.sync.Clone();
		}

		public FeedSyncSyndicationItem(FeedSyncSyndicationItem source, Sync sync)
			: base(source)
		{
			this.sync = sync;
		}

		public FeedSyncSyndicationItem(string title, string content, Uri itemAlternateLink) 
			: base(title, content, itemAlternateLink)
		{
		}

		public FeedSyncSyndicationItem(string title, string content, Uri itemAlternateLink, Sync sync)
			: base(title, content, itemAlternateLink)
		{
			Guard.ArgumentNotNull(sync, "sync");

			this.sync = sync;
			this.Id = sync.Id;
		}

		public FeedSyncSyndicationItem(string title, string summary, SyndicationContent content, Sync sync)
			: base()
		{
			Guard.ArgumentNotNullOrEmptyString(title, "title");
			Guard.ArgumentNotNull(content, "content");
			Guard.ArgumentNotNullOrEmptyString(summary, "summary");
			Guard.ArgumentNotNull(sync, "sync");

			this.Title = new TextSyndicationContent(title);
			this.Summary = new TextSyndicationContent(summary);
			this.Content = content;
			this.sync = sync;
			this.Id = sync.Id;
		}

		public FeedSyncSyndicationItem(string title, SyndicationContent content, Uri itemAlternateLink, string id, DateTimeOffset lastUpdatedTime)
			 : base(title, content, itemAlternateLink, id, lastUpdatedTime)
		{
		}

		public FeedSyncSyndicationItem(string title, SyndicationContent content, Uri itemAlternateLink, string id, DateTimeOffset lastUpdatedTime, Sync sync)
			: base(title, content, itemAlternateLink, id, lastUpdatedTime)
		{
			Guard.ArgumentNotNull(sync, "sync");

			this.sync = sync;
			this.Id = sync.Id;
		}

		public FeedSyncSyndicationItem(string title, string content, Uri itemAlternateLink, string id, DateTimeOffset lastUpdatedTime)
			: base(title, content, itemAlternateLink, id, lastUpdatedTime)
		{
		}

		public FeedSyncSyndicationItem(string title, string content, Uri itemAlternateLink, string id, DateTimeOffset lastUpdatedTime, Sync sync)
			: base(title, content, itemAlternateLink, id, lastUpdatedTime)
		{
			Guard.ArgumentNotNull(sync, "sync");

			this.sync = sync;
			this.Id = sync.Id;
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
				this.sync = Sync.Create(reader, version);
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
				sync.WriteXml(writer, version);
			}

			base.WriteElementExtensions(writer, version);
		}
	
	}
}
