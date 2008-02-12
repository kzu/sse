using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Xml;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.ServiceModel.Syndication;

namespace FeedSync.Tests
{
	[TestClass]
	public class SyncFixture : TestFixtureBase
	{
		protected virtual string GetVersion()
		{
			return new Rss20ItemFormatter().Version;
		}

		[TestMethod]
		public void ShouldEqualNullSyncToNull()
		{
			Assert.IsTrue((Sync)null == null);
		}

		[TestMethod]
		public void ShouldNotEqualNullOperator()
		{
			Assert.IsFalse(null == Sync.Create("foo", "foo", DateTime.Now));
			Assert.IsFalse(Sync.Create("foo", "foo", DateTime.Now) == null);
		}

		[TestMethod]
		public void ShouldNotEqualNull()
		{
			Assert.IsFalse(Sync.Create("foo", "foo", DateTime.Now).Equals((object)null));
			Assert.IsFalse(Sync.Create("foo", "foo", DateTime.Now).Equals((Sync)null));
		}

		[TestMethod]
		public void ShouldEqualIfSameId()
		{
			Sync s1 = Sync.Create(Guid.NewGuid().ToString(), "foo", DateTime.Now);
			Sync s2 = Sync.Create(s1.Id, s1.LastUpdate.By, s1.LastUpdate.When); ;

			Assert.AreEqual(s1, s2);
			Assert.IsTrue(s1 == s2);
		}

		[TestMethod]
		public void ShouldNotEqualIfDifferentId()
		{
			Sync s1 = Sync.Create(Guid.NewGuid().ToString(), "foo", DateTime.Now);
			Sync s2 = Sync.Create(Guid.NewGuid().ToString(), s1.LastUpdate.By, s1.LastUpdate.When); ;

			Assert.AreNotEqual(s1, s2);
			Assert.IsFalse(s1 == s2);
			Assert.IsTrue(s1 != s2);
		}

		[TestMethod]
		public void ShouldNotEqualIfDifferentBy()
		{
			Sync s1 = Sync.Create(Guid.NewGuid().ToString(), "foo", DateTime.Now);
			Sync s2 = Sync.Create(s1.Id, "Foo1", s1.LastUpdate.When); ;

			Assert.AreNotEqual(s1, s2);
			Assert.IsFalse(s1 == s2);
			Assert.IsTrue(s1 != s2);
		}

		[TestMethod]
		public void ShouldNotEqualIfDifferentWhen()
		{
			Sync s1 = Sync.Create(Guid.NewGuid().ToString(), "foo", DateTime.Now);
			Sync s2 = Sync.Create(s1.Id, s1.LastUpdate.By, DateTime.Now.AddMinutes(10));

			Assert.AreNotEqual(s1, s2);
			Assert.IsFalse(s1 == s2);
			Assert.IsTrue(s1 != s2);
		}

		[TestMethod]
		public void ShouldReadSyncWithConflict()
		{
			string xml = @"
   <sx:sync xmlns:sx='http://feedsync.org/2007/feedsync' id='0a7903db47fb0fff' updates='2'>
    <sx:history sequence='2' by='REO1750'/>
    <sx:history sequence='1' by='REO1750'/>
	<sx:conflicts>
	  <item>
	   <title>Buy icecream</title>
	   <customer id='1' />
	   <sx:sync id='0a7903db47fb0fff' updates='2'>
		 <sx:history sequence='2' by='JEO2000'/>
		 <sx:history sequence='1' by='REO1750'/>
	   </sx:sync>
	  </item>
		<item>
	   <title>Buy chocolate</title>
	   <customer id='1' />
	   <sx:sync id='1a7903db47fb0fff' updates='2'>
		 <sx:history sequence='2' by='JEO2000'/>
		 <sx:history sequence='1' by='REO1750'/>
	   </sx:sync>
	  </item>
	</sx:conflicts>
   </sx:sync>";

			XmlReader reader = GetReader(xml);
			reader.MoveToContent();

			Sync sync = Sync.Create(reader, GetVersion());
			
			int count = Count(sync.Conflicts);
			Assert.AreEqual(2, count);
		}

		[TestMethod]
		public void ShouldReadSync()
		{
			string xml = @"
   <sx:sync xmlns:sx='http://feedsync.org/2007/feedsync' id='0a7903db47fb0fff' updates='3'>
    <sx:history sequence='3' by='JEO2000'/>
    <sx:history sequence='2' by='REO1750'/>
    <sx:history sequence='1' by='REO1750'/>
   </sx:sync>";

			XmlReader reader = GetReader(xml);
			reader.MoveToContent();
			Sync sync = Sync.Create(reader, GetVersion());

			Assert.AreEqual("0a7903db47fb0fff", sync.Id);
			Assert.AreEqual(3, sync.Updates);
			Assert.AreEqual(3, Count(sync.UpdatesHistory));
			Assert.AreEqual(3, sync.LastUpdate.Sequence);
			Assert.AreEqual("JEO2000", sync.LastUpdate.By);
		}

		[TestMethod]
		public void ShouldWriteSync()
		{
			Sync sync = Sync.Create("0a7903db47fb0fff", "JEO2000", DateTime.Now);
			sync = sync.Update("REO1750", DateTime.Now);

			StringWriter sw = new StringWriter();
			XmlWriterSettings set = new XmlWriterSettings();
			set.Indent = true;
			XmlWriter xw = XmlWriter.Create(sw, set);

			sync.WriteXml(xw, GetVersion());

			xw.Flush();

			XmlElement output = GetElement(sw.ToString());

			Assert.AreEqual(1, EvaluateCount(output, "//sx:sync"));
			Assert.AreEqual(2, EvaluateCount(output, "//sx:sync/sx:history"));
		}


		[TestMethod]
		public void ShouldWriteSyncWithConflicts()
		{
			Sync sync = Sync.Create("0a7903db47fb0fff", "JEO2000", DateTime.Now);
			sync = sync.Update("REO1750", DateTime.Now);

			FeedSyncSyndicationItem item = new FeedSyncSyndicationItem("title", "content", new Uri("urn:test"),
				Sync.Create(Guid.NewGuid().ToString(), DeviceAuthor.Current, DateTime.Now));
			
			sync.Conflicts.Add(item);

			StringWriter sw = new StringWriter();
			XmlWriterSettings set = new XmlWriterSettings();
			set.Indent = true;
			XmlWriter xw = XmlWriter.Create(sw, set);

			sync.WriteXml(xw, GetVersion());

			xw.Flush();

			XmlElement output = GetElement(sw.ToString());

			Assert.AreEqual(2, EvaluateCount(output, "//sx:sync"));
			Assert.AreEqual(3, EvaluateCount(output, "//sx:sync/sx:history"));
		}


		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void ShouldFailReadWithInvalidNamespace()
		{
			string xml = @"
   <sx:sync xmlns:sx='http://feedsync.org/2007/sse' id='0a7903db47fb0fff' updates='3'>
   </sx:sync>";

			XmlReader reader = GetReader(xml);
			reader.MoveToContent();
			Sync sync = Sync.Create(reader, GetVersion());
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void ShouldFailReadWithInvalidElement()
		{
			string xml = @"
   <sx:foo xmlns:sx='http://feedsync.org/2007/FeedSync' id='0a7903db47fb0fff' updates='3'>
   </sx:foo>";

			XmlReader reader = GetReader(xml);
			reader.MoveToContent();
			Sync sync = Sync.Create(reader, GetVersion());
		}

	}
}
