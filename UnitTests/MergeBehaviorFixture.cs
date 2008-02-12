#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System;
using System.Text;
using System.Collections.Generic;
using System.Xml.XPath;
using System.IO;
using System.Threading;
using System.ServiceModel.Syndication;

namespace FeedSync.Tests
{
	[TestClass]
	public class MergeBehaviorFixture : TestFixtureBase
	{
		[TestMethod]
		public void ShouldWinLatestUpdateWithoutConflicts()
		{
			Sync sa = Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now);
			Sync sb = sa.Clone();

			sb = sb.Update("vga", DateTime.Now.AddSeconds(5));

			FeedSyncSyndicationItem originalItem = new FeedSyncSyndicationItem("a", "a", 
				new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), sa);
			originalItem.Id = sb.Id;
						
			FeedSyncSyndicationItem incomingItem = new FeedSyncSyndicationItem("b", "b",
				new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), sb);
			incomingItem.Id = sb.Id;
						
			MergeResult result = originalItem.Merge(incomingItem);

			Assert.AreEqual(MergeOperation.Updated, result.Operation);
			Assert.AreEqual("b", result.Proposed.Title.Text);
			Assert.AreEqual("vga", result.Proposed.Sync.LastUpdate.By);
		}

		[TestMethod]
		public void ShouldNoOpForEqualItem()
		{
			Sync sa = Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now);
			Sync sb = sa.Clone();

			FeedSyncSyndicationItem originalItem = new FeedSyncSyndicationItem("a", "a",
				new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), sa);
			originalItem.Id = sb.Id;
			
			FeedSyncSyndicationItem incomingItem = new FeedSyncSyndicationItem("a", "a",
				new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), sb);
			incomingItem.Id = sb.Id;
			
			MergeResult result = originalItem.Merge(incomingItem);

			Assert.AreEqual(MergeOperation.None, result.Operation);
		}

		[TestMethod]
		public void ShouldAddWithoutConflicts()
		{
			Sync sa = Sync.Create(Guid.NewGuid().ToString(), "kzu", null).Update("kzu", DateTime.Now);

			FeedSyncSyndicationItem incomingItem = new FeedSyncSyndicationItem("a", "a",
				new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), sa);
			
			MergeResult result = MergeBehavior.Merge(incomingItem);

			Assert.AreEqual(MergeOperation.Added, result.Operation);
		}

		[TestMethod]
		public void ShouldWinLatestUpdateWithConflicts()
		{
			Sync sa = Sync.Create(Guid.NewGuid().ToString(),
				"kzu", DateTime.Now.Subtract(TimeSpan.FromSeconds(10)));

			Sync sb = sa.Clone().Update("vga", DateTime.Now.AddSeconds(50));
			sa = sa.Update("kzu", DateTime.Now.AddSeconds(100));

			FeedSyncSyndicationItem originalItem = new FeedSyncSyndicationItem("a", "a",
				new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), sa);
			originalItem.Id = sb.Id;
			
			FeedSyncSyndicationItem incomingItem = new FeedSyncSyndicationItem("b", "b",
				new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), sb);
			incomingItem.Id = sb.Id;
			
			MergeResult result = originalItem.Merge(incomingItem);

			Assert.AreEqual(MergeOperation.Conflict, result.Operation);
			Assert.AreEqual("a", result.Proposed.Title.Text);
			Assert.AreEqual("kzu", result.Proposed.Sync.LastUpdate.By);
			Assert.AreEqual(1, result.Proposed.Sync.Conflicts.Count);
		}

		[TestMethod]
		public void ShouldWinLatestUpdateWithConflictsPreserved()
		{
			Sync sa = Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now);

			Sync sb = sa.Clone().Update("vga", DateTime.Now.AddSeconds(50));
			sa = sa.Update("kzu", DateTime.Now.AddSeconds(100));

			FeedSyncSyndicationItem originalItem = new FeedSyncSyndicationItem("a", "a",
				new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), sa);
			originalItem.Id = sb.Id;
			
			FeedSyncSyndicationItem incomingItem = new FeedSyncSyndicationItem("b", "b",
				new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), sb);
			incomingItem.Id = sb.Id;
			
			MergeResult result = originalItem.Merge(incomingItem);
			Assert.AreEqual(MergeOperation.Conflict, result.Operation);
			Assert.AreEqual("a", result.Proposed.Title.Text);
			Assert.AreEqual("kzu", result.Proposed.Sync.LastUpdate.By);
			Assert.AreEqual(1, result.Proposed.Sync.Conflicts.Count);

			// Merge the winner with conflict with the local no-conflict one.
			// Should be an update.
			result = originalItem.Merge(result.Proposed);

			Assert.AreEqual(MergeOperation.Conflict, result.Operation);
			Assert.AreEqual("a", result.Proposed.Title.Text);
			Assert.AreEqual("kzu", result.Proposed.Sync.LastUpdate.By);
			Assert.AreEqual(1, result.Proposed.Sync.Conflicts.Count);
		}
	}
}