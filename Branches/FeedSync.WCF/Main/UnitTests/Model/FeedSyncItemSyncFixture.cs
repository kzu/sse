using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ServiceModel.Syndication;
using System.IO;
using System.Xml;
using FeedSync.Tests;

namespace FeedSync.Model
{
	[TestClass]
	public class FeedSyncItemSyncFixture : TestFixtureBase
	{
		public FeedSyncItemSyncFixture()
		{
		}

		[TestMethod]
		public void ShouldCreateInstanceWithXmlContent()
		{
			FeedSyncSyndicationItem item = new FeedSyncSyndicationItem("title", "description",
				new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), 
				Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now));

			Assert.AreEqual("title", item.Title.Text);
			Assert.AreEqual("description", item.Summary.Text);
			Assert.IsInstanceOfType(item.Content, typeof(TextSyndicationContent));
			Assert.AreEqual("<payload/>", ((TextSyndicationContent)item.Content).Text);
		}

		[TestMethod]
		public void ShouldReadItem()
		{
			string xml = @"<item>
				<link>urn:test</link>
				<title>title</title>
				<description>content</description>
				<sx:sync id=""daa31e24-cced-4113-81ab-d1cf6f6c8f84"" updates=""1"" deleted=""false"" noconflicts=""false"" 
					xmlns:sx=""http://feedsync.org/2007/feedsync"">
					<sx:history sequence=""1"" when=""2008-02-11T18:43:49-02:00"" by=""PCI-MOBILE"" />
				</sx:sync>
			</item>";

			XmlReader reader = GetReader(xml);
			reader.MoveToContent();

			Rss20ItemFormatter<FeedSyncSyndicationItem> formatter = new Rss20ItemFormatter<FeedSyncSyndicationItem>();
			formatter.ReadFrom(reader);

			Assert.IsNotNull(formatter.Item);

			FeedSyncSyndicationItem item = (FeedSyncSyndicationItem)formatter.Item;
			Assert.IsNotNull(item.Sync);
		}

		[TestMethod]
		public void ShouldWriteItem()
		{
			FeedSyncSyndicationItem item = new FeedSyncSyndicationItem("title", "content", new Uri("urn:test"),
				Sync.Create(Guid.NewGuid().ToString(), DeviceAuthor.Current, DateTime.Now));
			
			StringWriter sw = new StringWriter();
			XmlWriterSettings set = new XmlWriterSettings();
			set.Indent = true;
			XmlWriter xw = XmlWriter.Create(sw, set);

			Rss20ItemFormatter<FeedSyncSyndicationItem> formatter = new Rss20ItemFormatter<FeedSyncSyndicationItem>(item);
			formatter.WriteTo(xw);

			xw.Flush();

			XmlElement output = GetElement(sw.ToString());

			Assert.AreEqual(1, EvaluateCount(output, "//sx:sync"));
			Assert.AreEqual(1, EvaluateCount(output, "//sx:sync/sx:history"));
		}

	}
}
