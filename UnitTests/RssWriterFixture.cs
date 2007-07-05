#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Xml.XPath;

namespace SimpleSharing.Tests
{
	[TestClass]
	public class RssWriterFixture : TestFixtureBase
	{
		[TestMethod]
		public void ShouldWriteEmptyItemPayload()
		{
			StringWriter sw = new StringWriter();
			XmlWriterSettings set = new XmlWriterSettings();
			set.Indent = true;
			XmlWriter xw = XmlWriter.Create(sw, set);

			Feed feed = new Feed("Hello World", "http://kzu", "this is my feed");
			feed.Sharing.Expires = new DateTime(2007, 6, 7);
			feed.Sharing.Related.Add(new Related("http://kzu/full", RelatedType.Complete, "Complete feed"));

			FeedWriter writer = new RssFeedWriter(xw);

			writer.Write(feed, new Item(new NullXmlItem("1", null), new Sync("1"))); 

			xw.Flush();
		}

		[TestMethod]
		public void ShouldWriteCompleteFeed()
		{
			MockSyncRepository syncRepo = new MockSyncRepository();
			MockXmlRepository xmlRepo = new MockXmlRepository();
			xmlRepo.AddThreeItemsByDays();
			SyncEngine engine = new SyncEngine(xmlRepo, syncRepo);

			StringWriter sw = new StringWriter();
			XmlWriterSettings set = new XmlWriterSettings();
			set.Indent = true;
			XmlWriter xw = XmlWriter.Create(sw, set);

			Feed feed = new Feed("Hello World", "http://kzu", "this is my feed");
			feed.Sharing.Expires = new DateTime(2007, 6, 7);
			feed.Sharing.Related.Add(new Related("http://kzu/full", RelatedType.Complete, "Complete feed"));

			FeedWriter writer = new RssFeedWriter(xw);
			engine.Publish(feed, writer);

			xw.Flush();

			XmlElement output = GetElement(sw.ToString());

			Assert.AreEqual(1, EvaluateCount(output, "/rss/channel"));
			Assert.AreEqual(1, EvaluateCount(output, "/rss/channel/sx:sharing"));
			Assert.AreEqual(Timestamp.ToString(feed.Sharing.Expires.Value), EvaluateString(output, "/rss/channel/sx:sharing/@expires"));
			Assert.AreEqual("http://kzu/full", EvaluateString(output, "/rss/channel/sx:sharing/sx:related/@link"));
			Assert.AreEqual("Complete feed", EvaluateString(output, "/rss/channel/sx:sharing/sx:related/@title"));
			Assert.AreEqual(3, EvaluateCount(output, "/rss/channel/item"));
			Assert.AreEqual(3, EvaluateCount(output, "/rss/channel/item/sx:sync"));
			Assert.AreEqual(3, EvaluateCount(output, "/rss/channel/item/sx:sync/sx:history"));
			Assert.AreEqual(3, EvaluateCount(output, "/rss/channel/item/sx:sync/sx:history[@sequence=1]"));
		}

		[TestMethod]
		public void ShouldPublishLast1Day()
		{
			MockSyncRepository syncRepo = new MockSyncRepository();
			MockXmlRepository xmlRepo = new MockXmlRepository();
			xmlRepo.AddThreeItemsByDays();
			SyncEngine engine = new SyncEngine(xmlRepo, syncRepo);

			StringWriter sw = new StringWriter();
			XmlWriterSettings set = new XmlWriterSettings();
			set.Indent = true;
			XmlWriter xw = XmlWriter.Create(sw, set);

			Feed feed = new Feed("Hello World", "http://kzu", "this is my feed");
			feed.Sharing.Related.Add(new Related("http://kzu/full", RelatedType.Complete));

			FeedWriter writer = new RssFeedWriter(xw);
			engine.Publish(feed, writer, 1);

			xw.Flush();

			XmlNode output = GetElement(sw.ToString());

			Assert.AreEqual(1, EvaluateCount(output, "/rss/channel"));
			Assert.AreEqual(1, EvaluateCount(output, "/rss/channel/sx:sharing"));
			Assert.AreEqual("http://kzu/full", EvaluateString(output, "/rss/channel/sx:sharing/sx:related/@link"));
			// We get two items, as the one which was created for yesterday, 
			// would be in the middle of the day, while the "last N days" logic 
			// is since the starting of yesterday.
			Assert.AreEqual(2, EvaluateCount(output, "/rss/channel/item"));
			Assert.AreEqual(2, EvaluateCount(output, "/rss/channel/item/sx:sync"));
			Assert.AreEqual(2, EvaluateCount(output, "/rss/channel/item/sx:sync/sx:history"));
			Assert.AreEqual(2, EvaluateCount(output, "/rss/channel/item/sx:sync/sx:history[@sequence=1]"));
		}

		[TestMethod]
		public void ShouldWriteFeedWithDeletedItem()
		{
			IXmlRepository xmlRepo = new MockXmlRepository().AddOneItem();
			SyncEngine engine = new SyncEngine(
				xmlRepo,
				new MockSyncRepository());

			Item item = GetFirst<Item>(engine.Export());
			xmlRepo.Remove(item.XmlItem.Id);

			StringWriter sw = new StringWriter();
			XmlWriterSettings set = new XmlWriterSettings();
			set.Indent = true;
			XmlWriter xw = XmlWriter.Create(sw, set);

			Feed feed = new Feed("Hello World", "http://kzu", "this is my feed");
			feed.Sharing.Related.Add(new Related("http://kzu/full", RelatedType.Complete));

			FeedWriter writer = new RssFeedWriter(xw);
			engine.Publish(feed, writer);

			xw.Flush();

			XmlNode output = GetElement(sw.ToString());

			Assert.AreEqual(1, EvaluateCount(output, "/rss/channel"));
			Assert.AreEqual(1, EvaluateCount(output, "/rss/channel/sx:sharing"));
			Assert.AreEqual("http://kzu/full", EvaluateString(output, "/rss/channel/sx:sharing/sx:related/@link"));
			Assert.AreEqual(1, EvaluateCount(output, "/rss/channel/item"));
			Assert.AreEqual(1, EvaluateCount(output, "/rss/channel/item/sx:sync"));
			Assert.AreEqual(true, XmlConvert.ToBoolean(EvaluateString(output, "/rss/channel/item/sx:sync/@deleted")));
		}

		[TestMethod]
		public void ShouldWriteHistoryWithNullWhen()
		{
			StringWriter sw = new StringWriter();
			XmlWriterSettings set = new XmlWriterSettings();
			set.Indent = true;
			XmlWriter xw = XmlWriter.Create(sw, set);

			Feed feed = new Feed("Hello World", "http://kzu", "this is my feed");
			feed.Sharing.Related.Add(new Related("http://kzu/full", RelatedType.Complete));

			XmlElement payload = new XmlDocument().CreateElement("payload");
			payload.InnerXml = "<geo:point xmlns:geo='http://geo'>25</geo:point>";

			Item item = new Item(
				new XmlItem("foo", "bar", payload),
				Behaviors.Create("1", "kzu", null, false));

			FeedWriter writer = new RssFeedWriter(xw);
			writer.Write(feed, item);

			xw.Flush();
		}

		[TestMethod]
		public void ShouldNotWrapPayload()
		{
			StringWriter sw = new StringWriter();
			XmlWriterSettings set = new XmlWriterSettings();
			set.Indent = true;
			XmlWriter xw = XmlWriter.Create(sw, set);

			Feed feed = new Feed("Hello World", "http://kzu", "this is my feed");
			feed.Sharing.Related.Add(new Related("http://kzu/full", RelatedType.Complete));

			XmlElement payload = new XmlDocument().CreateElement("payload");
			payload.InnerXml = "<geo:point xmlns:geo='http://geo'>25</geo:point>";

			Item item = new Item(
				new XmlItem("foo", "bar", payload),
				Behaviors.Create("1", "kzu", null, false));

			FeedWriter writer = new RssFeedWriter(xw);
			writer.Write(feed, item);

			xw.Flush();

			Assert.IsFalse(sw.ToString().Contains("<payload>"));
		}
	}
}
