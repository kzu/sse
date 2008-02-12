#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.ServiceModel.Syndication;

namespace FeedSync.Tests
{
	[TestClass]
	public class BehaviorFixture : TestFixtureBase
	{
		#region Merge

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void MergeShouldThrowIfIncomingItemNull()
		{
			new FeedSyncSyndicationItem().Merge(null);
		}

		[TestMethod]
		public void MergeShouldAddWithoutConflict()
		{
			Sync sync = Sync.Create(Guid.NewGuid().ToString(), "mypc\\user", DateTime.Now);

			FeedSyncSyndicationItem remoteItem = new FeedSyncSyndicationItem(
				"foo", "bar",
				new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml), sync);
			remoteItem.Id = sync.Id;
			
			MergeResult result = MergeBehavior.Merge(remoteItem);

			Assert.AreEqual(MergeOperation.Added, result.Operation);
			Assert.IsNotNull(result.Proposed);
		}

		[TestMethod]
		public void MergeShouldUpdateWithoutConflict()
		{
			Sync sync = Sync.Create(Guid.NewGuid().ToString(), "mypc\\user", DateTime.Now.Subtract(TimeSpan.FromMinutes(1)));
			FeedSyncSyndicationItem originalItem = new FeedSyncSyndicationItem(
				"foo", "bar",
				new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml), sync);
			originalItem.Id = sync.Id;

			// Simulate editing.
			sync = originalItem.Sync.Update("REMOTE\\kzu", DateTime.Now);
			FeedSyncSyndicationItem incomingItem = new FeedSyncSyndicationItem("changed", originalItem.Summary.Text, originalItem.Content,
					sync);
			
			MergeResult result = originalItem.Merge(incomingItem);

			Assert.AreEqual(MergeOperation.Updated, result.Operation);
			Assert.IsNotNull(result.Proposed);
			Assert.AreEqual("changed", result.Proposed.Title.Text);
			Assert.AreEqual("REMOTE\\kzu", result.Proposed.Sync.LastUpdate.By);
		}

		[TestMethod]
		public void MergeShouldDeleteWithoutConflict()
		{
			Sync sync = Sync.Create(Guid.NewGuid().ToString(), "mypc\\user", DateTime.Now.Subtract(TimeSpan.FromMinutes(1)));
			string id = sync.Id;
			FeedSyncSyndicationItem originalItem = new FeedSyncSyndicationItem(
				"foo", "bar",
				new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml), sync);
			originalItem.Id = id;
			
			// Simulate editing.
			sync = originalItem.Sync.Delete("REMOTE\\kzu", DateTime.Now);
			
			FeedSyncSyndicationItem incomingItem = new FeedSyncSyndicationItem("foo", "bar", originalItem.Content, sync);
			
			MergeResult result = originalItem.Merge(incomingItem);

			Assert.AreEqual(MergeOperation.Updated, result.Operation);
			Assert.IsNotNull(result.Proposed);
			Assert.AreEqual(true, result.Proposed.Sync.Deleted);
			Assert.AreEqual("REMOTE\\kzu", result.Proposed.Sync.LastUpdate.By);
		}

		[TestMethod]
		public void MergeShouldConflictOnDeleteWithConflict()
		{
			Sync localSync = Sync.Create(Guid.NewGuid().ToString(), 
				"mypc\\user", 
				DateTime.Now.Subtract(TimeSpan.FromMinutes(2)));
			
			FeedSyncSyndicationItem originalItem = new FeedSyncSyndicationItem(
				"foo", "bar", new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				localSync);

			FeedSyncSyndicationItem incomingItem = (FeedSyncSyndicationItem)originalItem.Clone();

			// Local editing.
			localSync = originalItem.Sync.Update("mypc\\user", DateTime.Now.Subtract(TimeSpan.FromMinutes(1)));
			originalItem = new FeedSyncSyndicationItem("changed", originalItem.Summary.Text,
				originalItem.Content,
				localSync);
			originalItem.Id = localSync.Id;

			// Remote editing.
			Sync remoteSync = incomingItem.Sync.Delete("REMOTE\\kzu", DateTime.Now);
			
			incomingItem = new FeedSyncSyndicationItem("foo", "bar", originalItem.Content, remoteSync);
			incomingItem.Id = originalItem.Id;

			// Merge conflicting changed incoming item.
			MergeResult result = originalItem.Merge(incomingItem);

			Assert.AreEqual(MergeOperation.Conflict, result.Operation);
			Assert.IsNotNull(result.Proposed);
			Assert.AreEqual(true, result.Proposed.Sync.Deleted);
			Assert.AreEqual("REMOTE\\kzu", result.Proposed.Sync.LastUpdate.By);
		}

		[TestMethod]
		public void MergeShouldNoOpWithNoChanges()
		{
			Sync sync = Sync.Create(Guid.NewGuid().ToString(), "mypc\\user", DateTime.Now);
			FeedSyncSyndicationItem item = new FeedSyncSyndicationItem("foo", "bar",
				new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				sync);
			item.Id = sync.Id;

			// Do a merge with the same item.
			MergeResult result = item.Merge(item);

			Assert.AreEqual(MergeOperation.None, result.Operation);
			Assert.IsNull(result.Proposed);
		}

		[TestMethod]
		public void MergeShouldNoOpOnUpdatedLocalItemWithUnchangedIncoming()
		{
			Sync sync = Sync.Create(Guid.NewGuid().ToString(), "mypc\\user", DateTime.Now.Subtract(TimeSpan.FromMinutes(1)));
			FeedSyncSyndicationItem originalItem = new FeedSyncSyndicationItem("foo", "title",
				new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				sync);

			FeedSyncSyndicationItem incomingItem = (FeedSyncSyndicationItem)originalItem.Clone();

			// Simulate editing.
			sync = originalItem.Sync.Update("mypc\\user", DateTime.Now);
			originalItem = new FeedSyncSyndicationItem("changed", originalItem.Summary.Text,
				originalItem.Content,
				sync);

			// Merge with the older incoming item.
			MergeResult result = originalItem.Merge(incomingItem);

			Assert.AreEqual(MergeOperation.None, result.Operation);
			Assert.IsNull(result.Proposed);
		}

		[TestMethod]
		public void MergeShouldIncomingWinWithConflict()
		{
			Sync localSync = Sync.Create(Guid.NewGuid().ToString(), "mypc\\user", DateTime.Now.Subtract(TimeSpan.FromMinutes(2)));
			FeedSyncSyndicationItem originalItem = new FeedSyncSyndicationItem("foo", "bar",
				new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				localSync);

			FeedSyncSyndicationItem incomingItem = (FeedSyncSyndicationItem)originalItem.Clone();

			// Local editing.
			localSync = originalItem.Sync.Update("mypc\\user", DateTime.Now.Subtract(TimeSpan.FromMinutes(1)));
			originalItem = new FeedSyncSyndicationItem("changed", originalItem.Summary.Text,
				originalItem.Content,
				localSync);

			// Remote editing.
			Sync remoteSync = incomingItem.Sync.Update("REMOTE\\kzu", DateTime.Now);
			incomingItem = new FeedSyncSyndicationItem("changed2", originalItem.Summary.Text,
				originalItem.Content,
				remoteSync);

			// Merge conflicting changed incoming item.
			MergeResult result = originalItem.Merge(incomingItem);

			Assert.AreEqual(MergeOperation.Conflict, result.Operation);
			Assert.IsNotNull(result.Proposed);
			// Remote item won
			Assert.AreEqual("REMOTE\\kzu", result.Proposed.Sync.LastUpdate.By);
			Assert.AreEqual(1, result.Proposed.Sync.Conflicts.Count);
			Assert.AreEqual("mypc\\user", result.Proposed.Sync.Conflicts[0].Sync.LastUpdate.By);
		}

		[TestMethod]
		public void MergeShouldLocalWinWithConflict()
		{
			Sync localSync = Sync.Create(Guid.NewGuid().ToString(), "mypc\\user", DateTime.Now.Subtract(TimeSpan.FromMinutes(2)));
			FeedSyncSyndicationItem originalItem = new FeedSyncSyndicationItem(
				"foo", "bar",
				new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				localSync);

			// Remote editing.
			Sync remoteSync = localSync.Update("REMOTE\\kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(1)));
			FeedSyncSyndicationItem incomingItem = new FeedSyncSyndicationItem("changed2", originalItem.Summary.Text,
				originalItem.Content,
				remoteSync);

			// Local editing.
			localSync = originalItem.Sync.Update("mypc\\user", DateTime.Now);
			originalItem = new FeedSyncSyndicationItem("changed", originalItem.Summary.Text,
				originalItem.Content,
				localSync);

			// Merge conflicting changed incoming item.
			MergeResult result = originalItem.Merge(incomingItem);

			Assert.AreEqual(MergeOperation.Conflict, result.Operation);
			Assert.IsNotNull(result.Proposed);
			// Local item won
			Assert.AreEqual("mypc\\user", result.Proposed.Sync.LastUpdate.By);
			Assert.AreEqual(1, result.Proposed.Sync.Conflicts.Count);
			Assert.AreEqual("REMOTE\\kzu", result.Proposed.Sync.Conflicts[0].Sync.LastUpdate.By);
		}

		[TestMethod]
		public void MergeShouldConflictWithDeletedLocalItem()
		{
			Sync localSync = Sync.Create(Guid.NewGuid().ToString(), DeviceAuthor.Current, DateTime.Now.Subtract(TimeSpan.FromMinutes(3)));
			
			FeedSyncSyndicationItem originalItem = new FeedSyncSyndicationItem(
				"foo", "bar",
				new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				localSync);

			// Remote editing.
			Sync remoteSync = originalItem.Sync.Update("REMOTE\\kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(1)));
			FeedSyncSyndicationItem incomingItem = new FeedSyncSyndicationItem(
				"changed2", originalItem.Summary.Text, originalItem.Content,
				remoteSync);

			localSync = localSync.Delete(DeviceAuthor.Current, DateTime.Now);
			originalItem = new FeedSyncSyndicationItem(originalItem, localSync);

			// Merge conflicting changed incoming item.
			MergeResult result = originalItem.Merge(incomingItem);

			Assert.AreEqual(MergeOperation.Conflict, result.Operation);
			Assert.IsNotNull(result.Proposed);
			// Local item won
			Assert.AreEqual(DeviceAuthor.Current, result.Proposed.Sync.LastUpdate.By);
			Assert.AreEqual(1, result.Proposed.Sync.Conflicts.Count);
			Assert.AreEqual("REMOTE\\kzu", result.Proposed.Sync.Conflicts[0].Sync.LastUpdate.By);
			Assert.IsTrue(result.Proposed.Sync.Deleted);
		}

		//// TODO:
		//// WinnerPicking missing tests: FirstWinsWithBy and comparison with updates.
		//// FirstWinsWithWhen when lastupdate.when is null

		//#endregion

		#region Update

		[TestMethod]
		public void UpdateShouldNotModifyArgument()
		{
			Sync expected = Sync.Create(Guid.NewGuid().ToString(), "foo", null);

			Sync updated = expected.Update("bar", null);

			Assert.AreEqual("foo", expected.LastUpdate.By);
			Assert.AreNotEqual(expected, updated);
			Assert.AreEqual("bar", updated.LastUpdate.By);
		}

		[TestMethod]
		public void UpdateShouldIncrementUpdatesByOne()
		{
			Sync sync = Sync.Create(Guid.NewGuid().ToString(), "kzu", null);

			int original = sync.Updates;

			Sync updated = sync.Update("foo", DateTime.Now);

			Assert.AreEqual(original + 1, updated.Updates);
		}

		[TestMethod]
		public void UpdateShouldAddTopmostHistory()
		{
			Sync sync = Sync.Create(Guid.NewGuid().ToString(), "kzu", null);

			int original = sync.Updates;

			sync = sync.Update("foo", DateTime.Now);
			sync = sync.Update("bar", DateTime.Now);

			Assert.AreEqual("bar", GetFirst<History>(sync.UpdatesHistory).By);
		}

		#endregion

		#region Create

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void CreateShouldThrowExceptionIfIdNull()
		{
			Sync.Create(null, "mypc\\user", DateTime.Now);
		}

		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void CreateShouldThrowExceptionIfIdEmpty()
		{
			Sync.Create("", "mypc\\user", DateTime.Now);
		}

		[TestMethod]
		public void CreateShouldNotThrowIfNullByWithWhen()
		{
			Sync.Create(Guid.NewGuid().ToString(), null, DateTime.Now);
		}

		[TestMethod]
		public void CreateShouldNotThrowIfNullWhenWithBy()
		{
			Sync.Create(Guid.NewGuid().ToString(), "foo", null);
		}

		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void CreateShouldThrowIfNullWhenAndBy()
		{
			Sync.Create(Guid.NewGuid().ToString(), null, null);
		}

		[TestMethod]
		public void CreateShouldReturnSyncWithId()
		{
			Guid id = Guid.NewGuid();
			Sync sync = Sync.Create(id.ToString(), "mypc\\user", DateTime.Now);
			Assert.AreEqual(id.ToString(), sync.Id);
		}

		[TestMethod]
		public void CreateShouldReturnSyncWithUpdatesEqualsToOne()
		{
			Guid id = Guid.NewGuid();
			Sync sync = Sync.Create(id.ToString(), "mypc\\user", DateTime.Now);
			Assert.AreEqual(1, sync.Updates);
		}

		[TestMethod]
		public void CreateShouldHaveAHistory()
		{
			Guid id = Guid.NewGuid();
			Sync sync = Sync.Create(id.ToString(), "mypc\\user", DateTime.Now);
			List<History> histories = new List<History>(sync.UpdatesHistory);
			Assert.AreEqual(1, histories.Count);
		}

		[TestMethod]
		public void CreateShouldHaveHistorySequenceSameAsUpdateCount()
		{
			Guid id = Guid.NewGuid();
			Sync sync = Sync.Create(id.ToString(), "mypc\\user", DateTime.Now);
			History history = new List<History>(sync.UpdatesHistory)[0];
			Assert.AreEqual(sync.Updates, history.Sequence);
		}

		[TestMethod]
		public void CreateShouldHaveHistoryWhenEqualsToNow()
		{
			Guid id = Guid.NewGuid();
			DateTime time = DateTime.Now;
			Sync sync = Sync.Create(id.ToString(), "mypc\\user", DateTime.Now);
			History history = new List<History>(sync.UpdatesHistory)[0];
			DatesEqualWithoutMillisecond(time, history.When.Value);
		}

		#endregion

		#region Delete

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfByNull()
		{
			Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now).Delete(null, DateTime.Now);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfWhenParameterNull()
		{
			Sync.Create(Guid.NewGuid().ToString(), "mypc\\user", DateTime.Now).Delete("mypc\\user", null);
		}

		[TestMethod]
		public void ShouldIncrementUpdatesByOneOnDeletion()
		{
			Sync sync = Sync.Create(new Guid().ToString(), "kzu", DateTime.Now);
			int updates = sync.Updates;
			sync = sync.Delete("mypc\\user", DateTime.Now);
			Assert.AreEqual(updates + 1, sync.Updates);
		}

		[TestMethod]
		public void ShouldDeletionAttributeBeTrue()
		{
			Sync sync = Sync.Create(new Guid().ToString(), "kzu", DateTime.Now);
			sync = sync.Delete("mypc\\user", DateTime.Now);
			Assert.AreEqual(true, sync.Deleted);
		}

		#endregion Delete

		#region ResolveConflicts

		[TestMethod]
		public void ResolveShouldNotUpdateArgument()
		{
			FeedSyncSyndicationItem item = new FeedSyncSyndicationItem(
				"foo", "bar", new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				Sync.Create(Guid.NewGuid().ToString(), "one", DateTime.Now));

			FeedSyncSyndicationItem resolved = item.ResolveConflicts("two", DateTime.Now, false);

			Assert.AreNotSame(item, resolved);
		}

		[TestMethod]
		public void ResolveShouldUpdateEvenIfNoConflicts()
		{
			FeedSyncSyndicationItem item = new FeedSyncSyndicationItem(
				"foo", "bar", new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				Sync.Create(Guid.NewGuid().ToString(), "one", DateTime.Now));

			FeedSyncSyndicationItem resolved = item.ResolveConflicts("two", DateTime.Now, false);

			Assert.AreNotEqual(item, resolved);
			Assert.AreEqual(2, resolved.Sync.Updates);
			Assert.AreEqual("two", resolved.Sync.LastUpdate.By);
		}

		[TestMethod]
		public void ResolveShouldAddConflictItemHistoryWithoutIncrementingUpdates()
		{
			FeedSyncSyndicationItem item = new FeedSyncSyndicationItem("foo", "bar",
				new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				Sync.Create(Guid.NewGuid().ToString(), "one",
				DateTime.Now.Subtract(TimeSpan.FromMinutes(10))));
			
			Sync conflictSync = Sync.Create(item.Sync.Id, "two",
				DateTime.Now.Subtract(TimeSpan.FromHours(1)));
			item.Sync.Conflicts.Add(new FeedSyncSyndicationItem(item, conflictSync));

			FeedSyncSyndicationItem conflicItem = new FeedSyncSyndicationItem(item, item.Sync);
			FeedSyncSyndicationItem resolvedItem = conflicItem.ResolveConflicts("one", DateTime.Now, false);

			Assert.AreEqual(2, resolvedItem.Sync.Updates);
			Assert.AreEqual(3, Count(resolvedItem.Sync.UpdatesHistory));
		}

		[TestMethod]
		public void ResolveShouldRemoveConflicts()
		{
			FeedSyncSyndicationItem item = new FeedSyncSyndicationItem("foo", "bar",
				new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				Sync.Create(Guid.NewGuid().ToString(), "one",
				DateTime.Now.Subtract(TimeSpan.FromMinutes(10))));
			
			Sync conflictSync = Sync.Create(item.Sync.Id, "two",
				DateTime.Now.Subtract(TimeSpan.FromHours(1)));
			item.Sync.Conflicts.Add(new FeedSyncSyndicationItem(item, conflictSync));

			FeedSyncSyndicationItem conflicItem = new FeedSyncSyndicationItem(item, item.Sync);
			FeedSyncSyndicationItem resolvedItem = conflicItem.ResolveConflicts("one", DateTime.Now, false);

			Assert.AreEqual(0, resolvedItem.Sync.Conflicts.Count);
		}

		[TestMethod]
		public void ResolveShouldNotAddConflictItemHistoryIfSubsumed()
		{
			Sync sync = Sync.Create(Guid.NewGuid().ToString(), "one",
				DateTime.Now);
			FeedSyncSyndicationItem item = new FeedSyncSyndicationItem("foo", "bar",
				new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				sync);
			
			Sync conflictSync = sync.Clone();
			// Add subsuming update
			sync = item.Sync.Update("one", DateTime.Now.AddDays(1));

			conflictSync = conflictSync.Update("two", DateTime.Now.AddMinutes(5));

			sync.Conflicts.Add(new FeedSyncSyndicationItem(item, conflictSync));

			FeedSyncSyndicationItem conflicItem = new FeedSyncSyndicationItem(item, sync);
			FeedSyncSyndicationItem resolvedItem = conflicItem.ResolveConflicts("one", DateTime.Now, false);

			Assert.AreEqual(3, resolvedItem.Sync.Updates);
			// there would otherwise be 3 updates to the original item + 2 on the conflict.
			Assert.AreEqual(4, Count(resolvedItem.Sync.UpdatesHistory));
		}

		#endregion

		#region SparsePurge

		[TestMethod]
		public void PurgeShouldRemoveOlderSequence()
		{
			Sync s = Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now.Subtract(TimeSpan.FromDays(1)));
			s = s.Update("kzu", DateTime.Now);

			Assert.AreEqual(2, Count(s.UpdatesHistory));
			Assert.AreEqual(2, s.Updates);

			Sync purged = s.SparsePurge();

			Assert.AreEqual(1, Count(purged.UpdatesHistory));
		}

		[TestMethod]
		public void PurgeShouldPreserveHistoryOrder()
		{
			Sync s = Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now.Subtract(TimeSpan.FromDays(1)));
			s = s.Update("kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(30)));
			s = s.Update("vga", DateTime.Now);

			Sync purged = s.SparsePurge();

			Assert.AreEqual(2, Count(purged.UpdatesHistory));
			Assert.AreEqual("vga", purged.LastUpdate.By);
		}

		[TestMethod]
		public void PurgeShouldPreserveHistoryNoBy()
		{
			Sync s = Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now.Subtract(TimeSpan.FromDays(1)));
			s = s.Update("kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(30)));
			s = s.Update(null, DateTime.Now.Subtract(TimeSpan.FromMinutes(10)));
			DateTime lastWhen = Timestamp.Normalize(DateTime.Now.Subtract(TimeSpan.FromMinutes(5)));
			s = s.Update(null, lastWhen);

			Sync purged = s.SparsePurge();

			Assert.AreEqual(3, Count(purged.UpdatesHistory));
			Assert.AreEqual(null, purged.LastUpdate.By);
			Assert.AreEqual(lastWhen, purged.LastUpdate.When);
		}

		[TestMethod]
		public void PurgeShouldPreserveOtherSyncProperties()
		{
			Sync s = Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now.Subtract(TimeSpan.FromDays(1)));
			s = s.Update("kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(30)));
			s = s.Update("vga", DateTime.Now);

			// TODO: set other properties
			s.Conflicts.Add(new FeedSyncSyndicationItem("foo", "bar", 
				new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				Sync.Create("foo", "kzu", DateTime.Now)));

			Sync purged = s.SparsePurge();

			Assert.AreEqual(2, Count(purged.UpdatesHistory));
			Assert.AreEqual("vga", purged.LastUpdate.By);
			Assert.IsTrue(purged.NoConflicts);
			Assert.AreEqual(1, purged.Conflicts.Count);
		}

		#endregion

		#endregion

		private static void DatesEqualWithoutMillisecond(DateTime d1, DateTime d2)
		{
			Assert.AreEqual(d1.Year, d2.Year);
			Assert.AreEqual(d1.Month, d2.Month);
			Assert.AreEqual(d1.Date, d2.Date);
			Assert.AreEqual(d1.Hour, d2.Hour);
			Assert.AreEqual(d1.Minute, d2.Minute);
			Assert.AreEqual(d1.Second, d2.Second);
		}
	}
}