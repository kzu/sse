#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System;
using System.Collections.Generic;
using System.Threading;
using System.ServiceModel.Syndication;

namespace FeedSync.Tests
{
	[TestClass]
	public class SyncEngineFixture : TestFixtureBase
	{
		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfNullLeftRepo()
		{
			new SyncEngine(null, new MockRepository());
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfNullRightRepo()
		{
			new SyncEngine(new MockRepository(), null);
		}

		[TestMethod]
		public void ShouldAddNewItems()
		{
			MockRepository left = new MockRepository(
				CreateItem("fizz", Guid.NewGuid().ToString(), new History("kzu")));
			MockRepository right = new MockRepository(
				CreateItem("buzz", Guid.NewGuid().ToString(), new History("vga")));

			SyncEngine engine = new SyncEngine(left, right);

			IList<FeedSyncSyndicationItem> conflicts = engine.Synchronize();

			Assert.AreEqual(0, conflicts.Count);
			Assert.AreEqual(2, left.Items.Count);
			Assert.AreEqual(2, right.Items.Count);
		}

		[TestMethod]
		public void ShouldFilterItems()
		{
			MockRepository left = new MockRepository(
				CreateItem("fizz", Guid.NewGuid().ToString(), new History("kzu")));
			MockRepository right = new MockRepository(
				CreateItem("buzz", Guid.NewGuid().ToString(), new History("vga")));

			SyncEngine engine = new SyncEngine(left, right);

			ItemFilter filter = new ItemFilter(delegate(FeedSyncSyndicationItem item)
			{
				if (item.Title.Text == "fizz" || item.Title.Text == "buzz")
					return false;

				return true;
			});

			IList<FeedSyncSyndicationItem> conflicts = engine.Synchronize(filter);

			Assert.AreEqual(0, conflicts.Count);
			Assert.AreEqual(1, left.Items.Count);
			Assert.AreEqual(1, right.Items.Count);
		}

		[TestMethod]
		public void ShouldFilterItemsOnLeft()
		{
			MockRepository left = new MockRepository(
				CreateItem("fizz", Guid.NewGuid().ToString(), new History("kzu")));
			MockRepository right = new MockRepository(
				CreateItem("buzz", Guid.NewGuid().ToString(), new History("vga")));

			SyncEngine engine = new SyncEngine(left, right);

			ItemFilter filter = new ItemFilter(delegate(FeedSyncSyndicationItem item)
			{
				return false;
			}, delegate(FeedSyncSyndicationItem item)
			{
				return true;
			});

			IList<FeedSyncSyndicationItem> conflicts = engine.Synchronize(filter);

			Assert.AreEqual(0, conflicts.Count);
			Assert.AreEqual(2, left.Items.Count);
			Assert.AreEqual(1, right.Items.Count); //Left does not return any item
		}

		[TestMethod]
		public void ShouldFilterItemsOnRight()
		{
			MockRepository left = new MockRepository(
				CreateItem("fizz", Guid.NewGuid().ToString(), new History("kzu")));
			MockRepository right = new MockRepository(
				CreateItem("buzz", Guid.NewGuid().ToString(), new History("vga")));

			SyncEngine engine = new SyncEngine(left, right);

			ItemFilter filter = new ItemFilter(delegate(FeedSyncSyndicationItem item)
			{
				return true;
			}, delegate(FeedSyncSyndicationItem item)
			{
				return false;
			});

			IList<FeedSyncSyndicationItem> conflicts = engine.Synchronize(filter);

			Assert.AreEqual(0, conflicts.Count);
			Assert.AreEqual(1, left.Items.Count);
			Assert.AreEqual(2, right.Items.Count); 
		}


		[TestMethod]
		public void ShouldMergeChangesBothWays()
		{
			FeedSyncSyndicationItem a = CreateItem("fizz", Guid.NewGuid().ToString(), new History("kzu"));
			FeedSyncSyndicationItem b = CreateItem("buzz", Guid.NewGuid().ToString(), new History("vga"));

			MockRepository left = new MockRepository(
				new FeedSyncSyndicationItem(a, a.Sync.Update("kzu", DateTime.Now)), 
				b);

			MockRepository right = new MockRepository(
				a,
				new FeedSyncSyndicationItem(b, b.Sync.Update("vga", DateTime.Now)));

			SyncEngine engine = new SyncEngine(left, right);

			IList<FeedSyncSyndicationItem> conflicts = engine.Synchronize();

			Assert.AreEqual(0, conflicts.Count);
			Assert.AreEqual(2, right.Items[a.Sync.Id].Sync.Updates);
			Assert.AreEqual(2, left.Items[b.Sync.Id].Sync.Updates);
		}

		[TestMethod]
		public void ShouldMarkItemDeleted()
		{
			FeedSyncSyndicationItem a = CreateItem("fizz", Guid.NewGuid().ToString(), new History("kzu"));
			FeedSyncSyndicationItem b = CreateItem("buzz", Guid.NewGuid().ToString(), new History("vga"));

			MockRepository left = new MockRepository(a, b);
			MockRepository right = new MockRepository(
				a,
				new FeedSyncSyndicationItem(b, b.Sync.Update("vga", DateTime.Now)));

			SyncEngine engine = new SyncEngine(left, right);

			IList<FeedSyncSyndicationItem> conflicts = engine.Synchronize();

			Assert.AreEqual(0, conflicts.Count);
			Assert.AreEqual(1, Count(left.GetAll(delegate(FeedSyncSyndicationItem i) { return !i.Sync.Deleted; })));
		}

		[TestMethod]
		public void ShouldSynchronizeSince()
		{
			FeedSyncSyndicationItem a = CreateItem("fizz", Guid.NewGuid().ToString(), new History("kzu", DateTime.Now.Subtract(TimeSpan.FromDays(1))));
			FeedSyncSyndicationItem b = CreateItem("buzz", Guid.NewGuid().ToString(), new History("vga", DateTime.Now.Subtract(TimeSpan.FromDays(1))));

			MockRepository left = new MockRepository(a);
			MockRepository right = new MockRepository(b);

			SyncEngine engine = new SyncEngine(left, right);

			IList<FeedSyncSyndicationItem> conflicts = engine.Synchronize(DateTime.Now);

			Assert.AreEqual(0, conflicts.Count);
			Assert.AreEqual(1, left.Items.Count);
			Assert.AreEqual(1, right.Items.Count);
		}

		[TestMethod]
		public void ShouldGenerateConflict()
		{
			FeedSyncSyndicationItem a = CreateItem("fizz", Guid.NewGuid().ToString(), new History("kzu"));
			Thread.Sleep(1000);

			MockRepository left = new MockRepository(
				new FeedSyncSyndicationItem(a, a.Sync.Update("kzu", DateTime.Now)));
			Thread.Sleep(1000);

			MockRepository right = new MockRepository(
				new FeedSyncSyndicationItem(a, a.Sync.Update("vga", DateTime.Now)));

			SyncEngine engine = new SyncEngine(left, right);

			IList<FeedSyncSyndicationItem> conflicts = engine.Synchronize();

			Assert.AreEqual(1, conflicts.Count);
			Assert.AreEqual(1, left.Items[a.Sync.Id].Sync.Conflicts.Count);
			Assert.AreEqual(1, right.Items[a.Sync.Id].Sync.Conflicts.Count);
		}

		[TestMethod]
		public void ShouldImportUpdateWithConflictLeft()
		{
			MockRepository left = new MockRepository();
			MockRepository right = new MockRepository();
			SyncEngine engine = new SyncEngine(left, right);

			string id = Guid.NewGuid().ToString();
			Sync sync = Sync.Create(id, DeviceAuthor.Current, DateTime.Now.Subtract(TimeSpan.FromMinutes(2)));
			FeedSyncSyndicationItem item = new FeedSyncSyndicationItem(
				"foo", "bar",
					new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				sync);

			left.Add(item);
			right.Add(item);

			FeedSyncSyndicationItem incomingItem = new FeedSyncSyndicationItem(item);

			// Local editing.
			item = new FeedSyncSyndicationItem("changed", item.Summary.Text,
				item.Content,
				item.Sync.Update(DeviceAuthor.Current, DateTime.Now.Subtract(TimeSpan.FromMinutes(1))));

			left.Update(item);

			// Conflicting remote editing.
			incomingItem = new FeedSyncSyndicationItem("remote", item.Summary.Text,
				item.Content,
				incomingItem.Sync.Update("REMOTE\\kzu", DateTime.Now));

			right.Update(incomingItem);

			IList<FeedSyncSyndicationItem> conflicts = engine.Synchronize();

			Assert.AreEqual(1, conflicts.Count);
			Assert.AreEqual(1, Count(left.GetAll()));
			Assert.AreEqual("remote", left.Get(id).Title.Text);
			Assert.AreEqual("REMOTE\\kzu", left.Get(id).Sync.LastUpdate.By);

			Assert.AreEqual(1, Count(left.GetConflicts()));
			Assert.AreEqual(1, Count(right.GetConflicts()));
		}

		[TestMethod]
		public void ShouldCallMergeIfRepositorySupportsIt()
		{
			MockMergeRepository left = new MockMergeRepository();
			MockMergeRepository right = new MockMergeRepository();
			SyncEngine engine = new SyncEngine(left, right);

			engine.Synchronize();

			Assert.IsTrue(left.MergeCalled);
			Assert.IsTrue(right.MergeCalled);
		}

		[TestMethod]
		public void ShouldCallImportPreviewHandler()
		{
			bool left = false;
			bool right = false;
			bool none = false;
			int both = 0;
			MergeFilterHandler leftHandler = delegate(IRepository targetRepository, IEnumerable<MergeResult> mergedItems)
			{
				Assert.AreEqual("left", targetRepository.FriendlyName);
				left = true;
				return mergedItems;
			};
			MergeFilterHandler rightHandler = delegate(IRepository targetRepository, IEnumerable<MergeResult> mergedItems)
			{
				Assert.AreEqual("right", targetRepository.FriendlyName);
				right = true;
				return mergedItems;
			};
			MergeFilterHandler bothHandler = delegate(IRepository targetRepository, IEnumerable<MergeResult> mergedItems)
			{
				both++;
				return mergedItems;
			};
			MergeFilterHandler noneHandler = delegate(IRepository targetRepository, IEnumerable<MergeResult> mergedItems)
			{
				none = true;
				return mergedItems;
			};

			SyncEngine engine = new SyncEngine(new MockRepository("left"), new MockRepository("right"));

			engine.Synchronize(new MergeFilter(leftHandler, MergeFilterBehaviors.Left));
			Assert.IsTrue(left);

			engine.Synchronize(new MergeFilter(rightHandler, MergeFilterBehaviors.Right));
			Assert.IsTrue(right);

			engine.Synchronize(new MergeFilter(bothHandler, MergeFilterBehaviors.Both));
			Assert.AreEqual(2, both);

			engine.Synchronize(new MergeFilter(noneHandler, MergeFilterBehaviors.None));
			Assert.IsFalse(none);
		}

		[TestMethod]
		public void ShouldReportImportProgress()
		{
			MockRepository left = new MockRepository();
			MockRepository right = new MockRepository();
			SyncEngine engine = new SyncEngine(left, right);

			string id = Guid.NewGuid().ToString();
			Sync sync = Sync.Create(id, DeviceAuthor.Current, DateTime.Now.Subtract(TimeSpan.FromMinutes(2)));
			FeedSyncSyndicationItem item = new FeedSyncSyndicationItem(
				"foo", "bar",
					new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				sync);

			left.Add(item);

			id = Guid.NewGuid().ToString();
			sync = Sync.Create(id, DeviceAuthor.Current, DateTime.Now.Subtract(TimeSpan.FromMinutes(2)));
			item = new FeedSyncSyndicationItem(
				"foo", "bar",
				new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				sync);

			right.Add(item);

			int received = 0, sent = 0;

			engine.ItemReceived += delegate { received++; };
			engine.ItemSent += delegate { sent++; };

			engine.Synchronize();

			Assert.AreEqual(2, left.Items.Count);
			Assert.AreEqual(2, right.Items.Count);

			// Receives the item that was sent first plus the existing remote one.
			Assert.AreEqual(2, received);
			Assert.AreEqual(1, sent);
		}

		[TestMethod]
		public void ShouldNotSendReceivedItemIfModifiedBeforeSince()
		{
			MockRepository left = new MockRepository();
			MockRepository right = new MockRepository();
			SyncEngine engine = new SyncEngine(left, right);

			string id = Guid.NewGuid().ToString();
			Sync sync = Sync.Create(id, DeviceAuthor.Current, DateTime.Now.Subtract(TimeSpan.FromMinutes(2)));
			FeedSyncSyndicationItem item = new FeedSyncSyndicationItem(
				"foo", "bar",
					new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				sync);

			left.Add(item);

			id = Guid.NewGuid().ToString();
			sync = Sync.Create(id, DeviceAuthor.Current, DateTime.Now.Subtract(TimeSpan.FromDays(2)));
			item = new FeedSyncSyndicationItem(
				"foo", "bar",
					new TextSyndicationContent("<foo id='bar'/>", TextSyndicationContentKind.XHtml),
				sync);

			right.Add(item);

			int received = 0, sent = 0;

			engine.ItemReceived += delegate { received++; };
			engine.ItemSent += delegate { sent++; };

			engine.Synchronize(DateTime.Now.Subtract(TimeSpan.FromMinutes(5)));

			// No new item would have been received from target as it was modified in the past.
			Assert.AreEqual(1, left.Items.Count);
			// Local item was sent.
			Assert.AreEqual(2, right.Items.Count);
			// We would have received the same item we sent, as we're first 
			// sending and then receiving.
			Assert.AreEqual(1, received);
			Assert.AreEqual(1, sent);
		}

		//[TestMethod]
		//public void ShouldExportByDaysWithItemTimestampIfNoSyncLastUpdateWhen()
		//{
		//   MockXmlRepository xmlrepo = new MockXmlRepository();
		//   MockSyncRepository syncrepo = new MockSyncRepository();

		//   IXmlItem xi = new XmlItem("title", "description", new XmlDocument().CreateElement("payload"));
		//   xi.Id = Guid.NewGuid().ToString();
		//   Sync sync = Behaviors.Create(xi.Id, "kzu", DateTime.Now, false);
		//   Item item = new Item(xi, sync);

		//   xmlrepo.Add(xi);
		//   syncrepo.Save(sync);

		//   SyncEngine engine = new SyncEngine(xmlrepo, syncrepo);

		//   IEnumerable<Item> items = engine.Export(1);

		//   Assert.AreEqual(1, Count(items));
		//}

		//[ExpectedException(typeof(InvalidOperationException))]
		//[TestMethod]
		//public void ShouldThrowIfRepositoryDoesntUpdateTimestamp()
		//{
		//    ISyncRepository syncRepo = new MockSyncRepository();
		//    IXmlRepository xmlRepo = new MockXmlRepository().AddOneItem();
		//    SyncEngine engine = new SyncEngine(xmlRepo, syncRepo);

		//    IEnumerable<Item> items = engine.Export();

		//    ISyncRepository syncRepo2 = new MockSyncRepository();
		//    IXmlRepository xmlRepo2 = new NotUpdatingRepository();
		//    SyncEngine engine2 = new SyncEngine(xmlRepo2, syncRepo2);

		//    engine2.Import("mock", items);
		//}

		private FeedSyncSyndicationItem CreateItem(string title, string id, History history, params History[] otherHistory)
		{
			Sync sync = Sync.Create(id, history.By, history.When);
			foreach (History h in otherHistory)
			{
				sync = sync.Update(h.By, h.When);
			}

			return new FeedSyncSyndicationItem(title, "description", 
				new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), sync);
		}

		class MockMergeRepository : IRepository
		{
			public bool MergeCalled;

			#region IRepository Members

			public string FriendlyName
			{
				get { return "MockMerge"; }
			}

			public bool SupportsMerge
			{
				get { return true; }
			}

			public FeedSyncSyndicationItem Get(string id)
			{
				return null;
			}

			public IEnumerable<FeedSyncSyndicationItem> GetAll()
			{
				return new FeedSyncSyndicationItem[0];
			}

			public IEnumerable<FeedSyncSyndicationItem> GetAllSince(DateTime? since)
			{
				return new FeedSyncSyndicationItem[0];
			}

			public IEnumerable<FeedSyncSyndicationItem> GetConflicts()
			{
				return new FeedSyncSyndicationItem[0];
			}

			public void Delete(string id)
			{
			}

			public void Update(FeedSyncSyndicationItem item)
			{
			}

			public FeedSyncSyndicationItem Update(FeedSyncSyndicationItem item, bool resolveConflicts)
			{
                return item;
			}

			public IEnumerable<FeedSyncSyndicationItem> Merge(IEnumerable<FeedSyncSyndicationItem> items)
			{
				MergeCalled = true;
				return new List<FeedSyncSyndicationItem>();
			}

			public void Add(FeedSyncSyndicationItem item)
			{
			}

			#endregion

			#region IRepository Members

			public IEnumerable<FeedSyncSyndicationItem> GetAll(Predicate<FeedSyncSyndicationItem> filter)
			{
				return new FeedSyncSyndicationItem[0];
			}

			public IEnumerable<FeedSyncSyndicationItem> GetAllSince(DateTime? since, Predicate<FeedSyncSyndicationItem> filter)
			{
				return new FeedSyncSyndicationItem[0];
			}

			#endregion
        }
	}
}
