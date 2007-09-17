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
			//PreviewImportHandler 
		}


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

		class MockRepository : IRepository
		{
			public Dictionary<string, Item> Items = new Dictionary<string, Item>();

			public MockRepository(params Item[] items)
			{
				foreach (Item item in items)
				{
					Items.Add(item.Sync.Id, item);
				}
			}

			public string FriendlyName
			{
				get { return "Mock"; }
			}

			public bool SupportsMerge
			{
				get { return false; }
			}

			public Item Get(string id)
			{
				if (Items.ContainsKey(id))
					return Items[id].Clone();
				else
					return null;
			}

			public IEnumerable<Item> GetAll()
			{
				return Items.Values;
			}

			public IEnumerable<Item> GetAllSince(DateTime? since)
			{
				foreach (Item i in Items.Values)
				{
					if (i.Sync.LastUpdate.When >= since)
						yield return i.Clone();
				}
			}

			public void Delete(string id)
			{
				Items.Remove(id);
			}

			public void Update(Item item)
			{
				Item i = item.Clone();
				Items[item.Sync.Id] = i;
			}

			public IList<Item> Merge(IEnumerable<Item> items)
			{
				throw new NotSupportedException();
			}

			public void Add(Item item)
			{
				Items.Add(item.Sync.Id, item);
			}
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

			public void Delete(string id)
			{
			}

			public void Update(Item item)
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

		public class SyncEngine
		{
			IRepository left;
			IRepository right;

			public SyncEngine(IRepository left, IRepository right)
			{
				Guard.ArgumentNotNull(left, "left");
				Guard.ArgumentNotNull(right, "right");

				this.left = left;
				this.right = right;
			}

			private IEnumerable<ItemMergeResult> NullPreviewHandler(IRepository targetRepository,
				IEnumerable<ItemMergeResult> mergedItems)
			{
				return mergedItems;
			}

			public IList<Item> Synchronize()
			{
				return SynchronizeImpl(null, NullPreviewHandler, PreviewBehavior.None);
			}

			public IList<Item> Synchronize(PreviewImportHandler previewer, PreviewBehavior behavior)
			{
				return SynchronizeImpl(null, previewer, behavior);
			}

			public IList<Item> Synchronize(DateTime? since)
			{
				return SynchronizeImpl(since, NullPreviewHandler, PreviewBehavior.None);
			}

			public IList<Item> Synchronize(DateTime? since, PreviewImportHandler previewer, PreviewBehavior behavior)
			{
				return SynchronizeImpl(since, previewer, behavior);
			}

			private IList<Item> SynchronizeImpl(DateTime? since, PreviewImportHandler previewer, PreviewBehavior behavior)
			{
				Guard.ArgumentNotNull(previewer, "previewer");

				IEnumerable<Item> incomingItems = (since == null) ? right.GetAll() : right.GetAllSince(since);

				// If repository supports its own SSE merge behavior, don't apply it locally.
				if (!left.SupportsMerge)
				{
					IEnumerable<ItemMergeResult> incomingToMerge = MergeItems(incomingItems, left);
					if (behavior == PreviewBehavior.Left || behavior == PreviewBehavior.Both)
					{
						incomingToMerge = previewer(left, incomingToMerge);
					}
					Import(incomingToMerge, left);
				}
				else
				{
					left.Merge(incomingItems);
				}

				IEnumerable<Item> outgoingItems = (since == null) ? left.GetAll() : left.GetAllSince(since);

				if (!right.SupportsMerge)
				{
					IEnumerable<ItemMergeResult> outgoingToMerge = MergeItems(outgoingItems, right);
					if (behavior == PreviewBehavior.Right || behavior == PreviewBehavior.Both)
					{
						outgoingToMerge = previewer(right, outgoingToMerge);
					}
					return Import(outgoingToMerge, right);
				}
				else
				{
					return right.Merge(outgoingItems);
				}
			}

			private IEnumerable<ItemMergeResult> MergeItems(IEnumerable<Item> items, IRepository repository)
			{
				foreach (Item incoming in items)
				{
					Item original = repository.Get(incoming.Sync.Id);
					ItemMergeResult result = new MergeBehavior().Merge(original, incoming);

					if (result.Operation != MergeOperation.None)
						yield return result;
				}
			}

			private IList<Item> Import(IEnumerable<ItemMergeResult> items, IRepository repository)
			{
				// Straight import of data in merged results. 
				// Conflicting items are saved and also 
				// are returned for conflict resolution by the user or 
				// a custom component. MergeBehavior determines 
				// the winner element that is saved.
				// Conflicts are returned in a list because we need 
				// the full iteration over the merged items to be 
				// processed. If we returned an IEnumerable, we would 
				// depend on the client iterating it in order to 
				// actually import items, which is undesirable.
				List<Item> conflicts = new List<Item>();

				foreach (ItemMergeResult result in items)
				{
					if (result.Operation != MergeOperation.None &&
						result.Proposed != null &&
						result.Proposed.Sync.Conflicts.Count > 0)
					{
						conflicts.Add(result.Proposed);
					}

					switch (result.Operation)
					{
						case MergeOperation.Added:
							repository.Add(result.Proposed);
							break;
						case MergeOperation.Deleted:
							repository.Delete(result.Proposed.Sync.Id);
							break;
						case MergeOperation.Updated:
						case MergeOperation.Conflict:
							repository.Update(result.Proposed);
							break;
						case MergeOperation.None:
							break;
						default:
							throw new InvalidOperationException();
					}
				}

				return conflicts;
			}
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
