using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ServiceModel.Syndication;

namespace FeedSync
{
	/// <summary>
	/// </summary>
	[TestClass]
	public class SyndicationFormatterFactoryFixture
	{
		public SyndicationFormatterFactoryFixture()
		{
		}

		[TestMethod]
		public void ShouldGetRssItemFormatter()
		{
			SyndicationItemFormatter formatter = SyndicationFormatterFactory.CreateItemFormatter(new Rss20ItemFormatter().Version);
			
			Assert.IsNotNull(formatter);
			Assert.IsInstanceOfType(formatter, typeof(Rss20ItemFormatter<FeedSyncSyndicationItem>));
		}

		[TestMethod]
		public void ShouldGetRssItemFormatterForItem()
		{
			SyndicationItemFormatter formatter = SyndicationFormatterFactory.CreateItemFormatter(new Rss20ItemFormatter().Version, new FeedSyncSyndicationItem());

			Assert.IsNotNull(formatter);
			Assert.IsInstanceOfType(formatter, typeof(Rss20ItemFormatter<FeedSyncSyndicationItem>));
		}

		[TestMethod]
		public void ShouldGetAtomItemFormatter()
		{
			SyndicationItemFormatter formatter = SyndicationFormatterFactory.CreateItemFormatter(new Atom10ItemFormatter().Version);

			Assert.IsNotNull(formatter);
			Assert.IsInstanceOfType(formatter, typeof(Atom10ItemFormatter<FeedSyncSyndicationItem>));
		}

		[TestMethod]
		public void ShouldGetAtomItemFormatterForItem()
		{
			SyndicationItemFormatter formatter = SyndicationFormatterFactory.CreateItemFormatter(new Atom10ItemFormatter().Version, new FeedSyncSyndicationItem());

			Assert.IsNotNull(formatter);
			Assert.IsInstanceOfType(formatter, typeof(Atom10ItemFormatter<FeedSyncSyndicationItem>));
		}

		[TestMethod]
		[ExpectedException(typeof(NotSupportedException))]
		public void ShouldThrowExceptionForUnknownVersions()
		{
			SyndicationItemFormatter formatter = SyndicationFormatterFactory.CreateItemFormatter("foo");
		}
	}
}
