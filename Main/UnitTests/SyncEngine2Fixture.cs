#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System;
using System.Collections.Generic;
using System.Threading;

namespace SimpleSharing.Tests
{
	[TestClass]
	public class SyncEngine2Fixture : TestFixtureBase
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

			IList<Item> conflicts = engine.Synchronize();

			Assert.AreEqual(0, conflicts.Count);
			Assert.AreEqual(2, left.Items.Count);
			Assert.AreEqual(2, right.Items.Count);
		}

		[TestMethod]
		public void ShouldMergeChangesBothWays()
		{
			Item a = CreateItem("fizz", Guid.NewGuid().ToString(), new History("kzu"));
			Item b = CreateItem("buzz", Guid.NewGuid().ToString(), new History("vga"));

			MockRepository left = new MockRepository(
				new Item(a.XmlItem, a.Sync.Update("kzu", DateTime.Now)), 
				b);

			MockRepository right = new MockRepository(
				a,
				new Item(b.XmlItem, b.Sync.Update("vga", DateTime.Now)));

			SyncEngine engine = new SyncEngine(left, right);

			IList<Item> conflicts = engine.Synchronize();

			Assert.AreEqual(0, conflicts.Count);
			Assert.AreEqual(2, right.Items[a.Sync.Id].Sync.Updates);
			Assert.AreEqual(2, left.Items[b.Sync.Id].Sync.Updates);
		}

		[TestMethod]
		public void ShouldDeleteItem()
		{
			Item a = CreateItem("fizz", Guid.NewGuid().ToString(), new History("kzu"));
			Item b = CreateItem("buzz", Guid.NewGuid().ToString(), new History("vga"));

			MockRepository left = new MockRepository(a, b);

			MockRepository right = new MockRepository(
				a, 
				new Item(b.XmlItem, b.Sync.Update("vga", DateTime.Now, true)));

			SyncEngine engine = new SyncEngine(left, right);

			IList<Item> conflicts = engine.Synchronize();

			Assert.AreEqual(0, conflicts.Count);
			Assert.AreEqual(1, left.Items.Count);
		}

		[TestMethod]
		public void ShouldSynchronizeSince()
		{
			Item a = CreateItem("fizz", Guid.NewGuid().ToString(), new History("kzu", DateTime.Now.Subtract(TimeSpan.FromDays(1))));
			Item b = CreateItem("buzz", Guid.NewGuid().ToString(), new History("vga", DateTime.Now.Subtract(TimeSpan.FromDays(1))));

			MockRepository left = new MockRepository(a);
			MockRepository right = new MockRepository(b);

			SyncEngine engine = new SyncEngine(left, right);

			IList<Item> conflicts = engine.Synchronize(DateTime.Now);

			Assert.AreEqual(0, conflicts.Count);
			Assert.AreEqual(1, left.Items.Count);
			Assert.AreEqual(1, right.Items.Count);
		}

		[TestMethod]
		public void ShouldGenerateConflict()
		{
			Item a = CreateItem("fizz", Guid.NewGuid().ToString(), new History("kzu"));
			Thread.Sleep(1000);

			MockRepository left = new MockRepository(
				new Item(a.XmlItem, a.Sync.Update("kzu", DateTime.Now)));
			Thread.Sleep(1000);

			MockRepository right = new MockRepository(
				new Item(a.XmlItem, a.Sync.Update("vga", DateTime.Now)));

			SyncEngine engine = new SyncEngine(left, right);

			IList<Item> conflicts = engine.Synchronize();

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
			Sync sync = Behaviors.Create(id, DeviceAuthor.Current, DateTime.Now.Subtract(TimeSpan.FromMinutes(2)), false);
			Item item = new Item(
				new XmlItem(id, "foo", "bar",
					GetElement("<foo id='bar'/>")),
				sync);

			left.Add(item);
			right.Add(item);

			Item incomingItem = item.Clone();

			// Local editing.
			item = new Item(new XmlItem(id, "changed", item.XmlItem.Description,
				item.XmlItem.Payload),
				Behaviors.Update(item.Sync, DeviceAuthor.Current, DateTime.Now.Subtract(TimeSpan.FromMinutes(1)), false));

			left.Update(item);

			// Conflicting remote editing.
			incomingItem = new Item(new XmlItem(id, "remote", item.XmlItem.Description,
				item.XmlItem.Payload),
				Behaviors.Update(incomingItem.Sync, "REMOTE\\kzu", DateTime.Now, false));

			right.Update(incomingItem);
			
			IList<Item> conflicts = engine.Synchronize();

			Assert.AreEqual(1, conflicts.Count);
			Assert.AreEqual(1, Count(left.GetAll()));
			Assert.AreEqual("remote", left.Get(id).XmlItem.Title);
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
			PreviewImportHandler leftHandler = delegate(IRepository targetRepository, IEnumerable<ItemMergeResult> mergedItems)
			{
				Assert.AreEqual("left", targetRepository.FriendlyName);
				left = true;
				return mergedItems;
			};
			PreviewImportHandler rightHandler = delegate(IRepository targetRepository, IEnumerable<ItemMergeResult> mergedItems)
			{
				Assert.AreEqual("right", targetRepository.FriendlyName);
				right = true;
				return mergedItems;
			};
			PreviewImportHandler bothHandler = delegate(IRepository targetRepository, IEnumerable<ItemMergeResult> mergedItems)
			{
				both++;
				return mergedItems;
			};
			PreviewImportHandler noneHandler = delegate(IRepository targetRepository, IEnumerable<ItemMergeResult> mergedItems)
			{
				none = true;
				return mergedItems;
			};

			SyncEngine engine = new SyncEngine(new MockRepository("left"), new MockRepository("right"));

			engine.Synchronize(leftHandler, PreviewBehavior.Left);
			Assert.IsTrue(left);

			engine.Synchronize(rightHandler, PreviewBehavior.Right);
			Assert.IsTrue(right);

			engine.Synchronize(bothHandler, PreviewBehavior.Both);
			Assert.AreEqual(2, both);

			engine.Synchronize(noneHandler, PreviewBehavior.None);
			Assert.IsFalse(none);
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

		private Item CreateItem(string title, string id, History history, params History[] otherHistory)
		{
			XmlItem xml = new XmlItem(title, null, GetElement("<payload/>"));
			Sync sync = Behaviors.Create(id, history.By, history.When, false);
			foreach (History h in otherHistory)
			{
				sync = sync.Update(h.By, h.When);
			}

			return new Item(xml, sync);
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

			public Item Get(string id)
			{
				return null;
			}

			public IEnumerable<Item> GetAll()
			{
				return new Item[0];
			}

			public IEnumerable<Item> GetAllSince(DateTime? since)
			{
				return new Item[0];
			}

			public IEnumerable<Item> GetConflicts()
			{
				return new Item[0];
			}

			public void Delete(string id)
			{
			}

			public void Update(Item item)
			{
			}

			public void Update(Item item, bool resolveConflicts)
			{
			}

			public IList<Item> Merge(IEnumerable<Item> items)
			{
				MergeCalled = true;
				return new List<Item>();
			}

			public void Add(Item item)
			{
			}

			#endregion
		}

		/// <summary>
		///3.3	Merge Behavior
		///When a subscribing endpoint incorporates items from a publishing endpoint’s feed, these items must be merged with the existing local items. The act of merging items from an incoming feed detects new items, item updates and item conflicts and produces a merged result feed.  The merging of two items with the same id attribute value will result in a ‘winning’ item that MAY have conflict items.  In order to merge items, implementations MUST follow this algorithm for the two items:
		///1.	If no local item exists with the same id attribute value as the incoming item, add the incoming item to the merge result feed; we are done processing the incoming item.
		///2.	Create a collection L and populate it with the local item and the local item’s conflicts (if any exist) by using the following steps:
		///a.	For each item sub-element of the sx:conflicts element for the local item:
		///i.	Add the item sub-element to L
		///b.	If the local item has a sx:conflicts sub-element, remove it
		///c.	Add the local item to L
		///3.	Create a collection I and populate it with the incoming item and the incoming item’s conflicts (if any exist) by using the following steps:
		///a.	For each item sub-element of the sx:conflicts element for the incoming item:
		///i.	Add the item sub-element to I
		///b.	If the incoming item has a sx:conflicts sub-element, remove it
		///c.	Add the incoming item to I
		///4.	Create a collection M that will be used to contain items that will appear in the merged result feed
		///5.	Create a reference W for the current ‘winning’ item and set it to an unassigned value
		///6.	Using L as the outer collection and I as the inner collection, perform the following step
		///7.	For each item X in outer collection:
		///a.	For each item Y in inner collection:
		///i.	Determine if X is subsumed1 by Y – if so then remove X from the outer collection; process the next item in the outer collection
		///b.	Add X to M
		///c.	If W has not been assigned a value, set W to X; process the next item in the outer collection 
		///d.	Determine if X should be declared as the new ‘winning’ item3 – if so set W to X.
		///8.	Using I as the outer collection and L as the inner collection, perform step 7 again
		///9.	Add W to the merge result feed
		///10.	If the noconflicts attribute is set to true, then we are done processing
		///11.	If M contains more than one item:
		///a.	Create a sx:conflicts element and add it as a sub-element of the sx:sync element for W
		///b.	For each item Z in M:
		///i.	If Z equals W (i.e. they are the same item), then process the next item in M
		///ii.	Add Z as a sub-element of the sx:conflicts element created in step 11a.
		/// </summary>
		internal class MergeBehavior
		{
			public ItemMergeResult Merge(Item originalItem, Item incomingItem)
			{
				Guard.ArgumentNotNull(incomingItem, "incomingItem");

				Item incoming = incomingItem.Clone();

				if (originalItem == null)
				{
					return new ItemMergeResult(null, incoming, incoming, MergeOperation.Added);
				}

				Item original = originalItem.Clone();

				// History on both elements must have at least one entry
				if (original.Sync.LastUpdate == null ||
					incoming.Sync.LastUpdate == null)
				{
					throw new InvalidOperationException();
					//throw new ArgumentException(Properties.Resources.SyncHistoryRequired);
				}

				Item proposed;
				MergeOperation operation = MergeItems(original, incoming, out proposed);

				// If the sync are equals and there was no conflict (in these case the Sync might be 
				// equal as the proposed could be the original item, but with conflicts), then there's 
				// no merge to perform.
				if (proposed != null && proposed.Sync.Equals(original.Sync) && operation != MergeOperation.Conflict)
				{
					return new ItemMergeResult(original, incoming, null, MergeOperation.None);
				}
				else
				{
					return new ItemMergeResult(original, incoming, proposed, operation);
				}
			}

			private MergeOperation MergeItems(Item localItem, Item incomingItem, out Item proposedItem)
			{
				proposedItem = null;

				//3.3.2
				List<Item> L = new List<Item>();
				L.AddRange(localItem.Sync.Conflicts);
				localItem.Sync.Conflicts.Clear();
				L.Add(localItem);


				//3.3.3
				List<Item> I = new List<Item>();
				I.AddRange(incomingItem.Sync.Conflicts);
				incomingItem.Sync.Conflicts.Clear();
				I.Add(incomingItem);

				//3.3.4
				List<Item> M = new List<Item>();

				//3.3.5
				Item W = null;

				//3.3.6 and 3.3.7
				PerformStep7(ref L, ref I, M, ref W);
				//3.3.8
				PerformStep7(ref I, ref L, M, ref W);

				if (W == null)
				{
					//There is no need to update the local item
					return MergeOperation.None;
				}

				proposedItem = W;

				//3.3.10
				if (!W.Sync.NoConflicts)
				{
					//3.3.11
					foreach (Item z in M)
					{
						if (!z.Equals(W) && !W.Sync.Conflicts.Contains(z))
						{
							W.Sync.Conflicts.Add(z);
						}
					}
				}

				if (W.Sync.Conflicts.Count > 0)
				{
					return MergeOperation.Conflict;
				}
				else if (W.Sync.Deleted)
				{
					return MergeOperation.Deleted;
				}
				else
				{
					return MergeOperation.Updated;
				}
			}

			/// <summary>
			/// 3.3.7 implementation
			/// </summary>
			/// <param name="outerCollection"></param>
			/// <param name="innerCollection"></param>
			/// <param name="M"></param>
			/// <param name="W"></param>
			private void PerformStep7(ref List<Item> outerCollection, ref List<Item> innerCollection, List<Item> M, ref Item W)
			{
				List<Item> resOuter = new List<Item>(outerCollection);
				List<Item> resInner = new List<Item>(innerCollection);

				// Collections must be modified from this method.
				foreach (Item x in outerCollection)
				{
					bool isSubsumed = false;
					foreach (Item y in innerCollection)
					{
						if (x.IsSubsumedBy(y))
						{
							isSubsumed = true;
							resOuter.Remove(x);
							break;
						}
					}

					if (!isSubsumed)
					{
						M.Add(x);
						if (W == null)
						{
							W = x;
						}
						else
						{
							W = WinnerPicking(W, x);
						}
					}
				}

				outerCollection = resOuter;
				innerCollection = resInner;
			}

			/// <summary>
			///Winner Picking
			///The ‘winning’ item between an item X and an item Y is the item with most recent update , where X and Y are items with the same id attribute value.  In order to determine the ‘winning’ item, implementations MUST perform the following comparisons, in order, for X and Y:
			///1.	If X has a greater updates attribute value for the sx:sync sub-element than Y’s, then X is the ‘winning’ item
			///2.	If X has the same updates attribute value for the sx:sync sub-element as Y:
			///a.	If X has a when attribute for the topmost sx:history sub-element and Y does not, then X is the ‘winning’ item
			///b.	If X has a when attribute value for the topmost sx:history sub-element and that is chronologically later than Y’s, then X is the ‘winning’ item
			///c.	If X has the same when attribute for the topmost sx:history sub-element as Y:
			///i.	If X has a by attribute for the topmost sx:history sub-element and Y does not, then X is the ‘winning’ item 
			///ii.	If X has a by attribute value for the topmost sx:history sub-element that is collates greater (see Section 2.4 for collation rules) than Y’s, then X is the ‘winning’ item
			///3.	Y is the ‘winning’ item
			/// </summary>
			/// <param name="item"></param>
			/// <param name="anotherItem"></param>
			/// <returns></returns>
			private Item WinnerPicking(Item item, Item anotherItem)
			{
				Item winner = null;

				if (item.Sync.Updates == anotherItem.Sync.Updates)
				{
					if (!FirstWinsWithWhen(item, anotherItem, out winner))
					{
						FirstWinsWithWhen(anotherItem, item, out winner);
					}

					if (winner == null && !FirstWinsWithBy(item, anotherItem, out winner))
					{
						FirstWinsWithBy(anotherItem, item, out winner);
					}
				}
				else
				{
					winner = item.Sync.Updates > anotherItem.Sync.Updates ? item : anotherItem;
				}

				return winner;
			}

			private bool FirstWinsWithWhen(Item first, Item second, out Item winner)
			{
				winner = null;

				if (first.Sync.LastUpdate.When == null) return false;

				bool firstWins = second.Sync.LastUpdate.When == null ||
						(first.Sync.LastUpdate.When > second.Sync.LastUpdate.When);

				if (firstWins) winner = first;

				return firstWins;
			}

			private bool FirstWinsWithBy(Item first, Item second, out Item winner)
			{
				winner = null;

				if (first.Sync.LastUpdate.By == null) return false;

				bool firstWins = second.Sync.LastUpdate.By == null ||
						(second.Sync.LastUpdate.By != null &&
						!first.Sync.LastUpdate.By.Equals(second.Sync.LastUpdate.By) &&
						first.Sync.LastUpdate.By.Length > second.Sync.LastUpdate.By.Length
						);

				if (firstWins) winner = first;

				return firstWins;
			}
		}
	}
}
