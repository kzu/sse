#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Net;

namespace SimpleSharing.Tests
{
	[TestClass]
	public class MSLiveLabsFixture : TestFixtureBase
	{
		[Ignore]
		[TestMethod]
		public void ShouldUploadEmptyItems()
		{
			ISyncRepository syncRepo = new MockSyncRepository();
			IXmlRepository xmlRepo = new MockXmlRepository();
			SyncEngine engine = new SyncEngine(xmlRepo, syncRepo);

			WebRequest req = WebRequest.Create("http://sse.mslivelabs.com/feed.sse?i=293578659bbf40bfb8aa0b9102c36766&c=1&alt=RSS");
			req.Timeout = -1;
			req.Method = "PUT";

			XmlWriterSettings set = new XmlWriterSettings();
			set.CloseOutput = true;
			Feed feed = new Feed("Client feed", "http://client/feed", "Client feed description");
			using (XmlWriter w = XmlWriter.Create(req.GetRequestStream(), set))
			{
				engine.Publish(feed, new RssFeedWriter(w));
			}

			req.GetResponse();
		}

		[Ignore]
		[TestMethod]
		public void ShouldSyncItems()
		{
			ISyncRepository syncRepo = new MockSyncRepository();
			IXmlRepository xmlRepo = new MockXmlRepository();
			SyncEngine engine = new SyncEngine(xmlRepo, syncRepo);

			using (XmlReader xr = XmlReader.Create("http://sse.mslivelabs.com/feed.sse?i=293578659bbf40bfb8aa0b9102c36766&c=1&alt=Rss"))
			{
				IList<Item> conflicts = engine.Subscribe(new RssFeedReader(xr));

				Assert.AreEqual(0, conflicts.Count);
			}

			Assert.AreEqual(2, Count(xmlRepo.GetAll()));
		}

		[Ignore]
		[TestMethod]
		public void ShouldUpdateItems()
		{
			ISyncRepository syncRepo = new MockSyncRepository();
			IXmlRepository xmlRepo = new MockXmlRepository();
			SyncEngine engine = new SyncEngine(xmlRepo, syncRepo);

			using (XmlReader xr = XmlReader.Create("http://sse.mslivelabs.com/feed.sse?i=293578659bbf40bfb8aa0b9102c36766&c=1&alt=Rss"))
			{
				IList<Item> conflicts = engine.Subscribe(new RssFeedReader(xr));

				Assert.AreEqual(0, conflicts.Count);
			}

			IXmlItem first = GetFirst<IXmlItem>(xmlRepo.GetAll());
			first.Title = "Baz";
			xmlRepo.Update(first);

			WebRequest req = WebRequest.Create("http://sse.mslivelabs.com/feed.sse?i=293578659bbf40bfb8aa0b9102c36766&c=1&alt=RSS");
			req.Timeout = -1;
			req.Method = "PUT";

			XmlWriterSettings set = new XmlWriterSettings();
			set.CloseOutput = true;
			Feed feed = new Feed("Client feed", "http://client/feed", "Client feed description");
			using (XmlWriter w = XmlWriter.Create(req.GetRequestStream(), set))
			{
				engine.Publish(feed, new RssFeedWriter(w));
			}

			req.GetResponse();

			syncRepo = new MockSyncRepository();
			xmlRepo = new MockXmlRepository();
			engine = new SyncEngine(xmlRepo, syncRepo);

			using (XmlReader xr = XmlReader.Create("http://sse.mslivelabs.com/feed.sse?i=293578659bbf40bfb8aa0b9102c36766&c=1&alt=RSS"))
			{
				IList<Item> conflicts = engine.Subscribe(new RssFeedReader(xr));

				Assert.AreEqual(0, conflicts.Count);
			}

			Assert.AreEqual(2, Count(xmlRepo.GetAll()));

			first = GetFirst<IXmlItem>(xmlRepo.GetAll());
			Assert.AreEqual("Baz", first.Title);
		}
	}
}
