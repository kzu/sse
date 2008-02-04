using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ServiceModel.Syndication;

namespace FeedSync
{
	/// <summary>
	///3.3	Merge Behavior
	///When a subscribing endpoint incorporates items from a publishing endpoint�s feed, these items must be merged with the existing local items. The act of merging items from an incoming feed detects new items, item updates and item conflicts and produces a merged result feed.  The merging of two items with the same id attribute value will result in a �winning� item that MAY have conflict items.  In order to merge items, implementations MUST follow this algorithm for the two items:
	///1.	If no local item exists with the same id attribute value as the incoming item, add the incoming item to the merge result feed; we are done processing the incoming item.
	///2.	Create a collection L and populate it with the local item and the local item�s conflicts (if any exist) by using the following steps:
	///a.	For each item sub-element of the sx:conflicts element for the local item:
	///i.	Add the item sub-element to L
	///b.	If the local item has a sx:conflicts sub-element, remove it
	///c.	Add the local item to L
	///3.	Create a collection I and populate it with the incoming item and the incoming item�s conflicts (if any exist) by using the following steps:
	///a.	For each item sub-element of the sx:conflicts element for the incoming item:
	///i.	Add the item sub-element to I
	///b.	If the incoming item has a sx:conflicts sub-element, remove it
	///c.	Add the incoming item to I
	///4.	Create a collection M that will be used to contain items that will appear in the merged result feed
	///5.	Create a reference W for the current �winning� item and set it to an unassigned value
	///6.	Using L as the outer collection and I as the inner collection, perform the following step
	///7.	For each item X in outer collection:
	///a.	For each item Y in inner collection:
	///i.	Determine if X is subsumed1 by Y � if so then remove X from the outer collection; process the next item in the outer collection
	///b.	Add X to M
	///c.	If W has not been assigned a value, set W to X; process the next item in the outer collection 
	///d.	Determine if X should be declared as the new �winning� item3 � if so set W to X.
	///8.	Using I as the outer collection and L as the inner collection, perform step 7 again
	///9.	Add W to the merge result feed
	///10.	If the noconflicts attribute is set to true, then we are done processing
	///11.	If M contains more than one item:
	///a.	Create a sx:conflicts element and add it as a sub-element of the sx:sync element for W
	///b.	For each item Z in M:
	///i.	If Z equals W (i.e. they are the same item), then process the next item in M
	///ii.	Add Z as a sub-element of the sx:conflicts element created in step 11a.
	/// </summary>
	public static class MergeBehavior
	{
		/// <summary>
		/// Merges the two items applying the SSE algorithm.
		/// </summary>
		/// // TODO: Same problem as the Sync.GetConflicts problem, see how to serialize the conflicts in a transparent way
		public static MergeResult Merge(this FeedSyncSyndicationItem originalItem, FeedSyncSyndicationItem incomingItem, SyndicationItemFormatter formatter)
		{
			Guard.ArgumentNotNull(incomingItem, "incomingItem");

			FeedSyncSyndicationItem incoming = (FeedSyncSyndicationItem)incomingItem.Clone();

			if (originalItem == null)
			{
				return new MergeResult(null, incoming, incoming, MergeOperation.Added);
			}

			FeedSyncSyndicationItem original = (FeedSyncSyndicationItem)originalItem.Clone();

			// History on both elements must have at least one entry
			if (original.Sync.LastUpdate == null ||
				incoming.Sync.LastUpdate == null)
			{
				throw new ArgumentException(Properties.Resources.SyncHistoryRequired);
			}

			FeedSyncSyndicationItem proposed;
			MergeOperation operation = MergeItems(original, incoming, out proposed, formatter);

			// If the sync are equals and there was no conflict (in these case the Sync might be 
			// equal as the proposed could be the original item, but with conflicts), then there's 
			// no merge to perform.
			if (proposed != null && proposed.Sync.Equals(original.Sync) && operation != MergeOperation.Conflict)
			{
				return new MergeResult(original, incoming, null, MergeOperation.None);
			}
			else
			{
				return new MergeResult(original, incoming, proposed, operation);
			}
		}

		private static MergeOperation MergeItems(FeedSyncSyndicationItem localItem, FeedSyncSyndicationItem incomingItem, out FeedSyncSyndicationItem proposedItem, SyndicationItemFormatter formatter)
		{
			proposedItem = null;

			//3.3.2
			List<FeedSyncSyndicationItem> L = new List<FeedSyncSyndicationItem>();
			L.AddRange(localItem.Sync.GetConflicts<FeedSyncSyndicationItem>(formatter));
			localItem.Sync.RawConflicts.Clear();
			L.Add(localItem);
			
			//3.3.3
			List<FeedSyncSyndicationItem> I = new List<FeedSyncSyndicationItem>();
			I.AddRange(incomingItem.Sync.GetConflicts<FeedSyncSyndicationItem>(formatter));
			incomingItem.Sync.RawConflicts.Clear();
			I.Add(incomingItem);

			//3.3.4
			List<FeedSyncSyndicationItem> M = new List<FeedSyncSyndicationItem>();

			//3.3.5
			FeedSyncSyndicationItem W = null;

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
				List<FeedSyncSyndicationItem> conflictsW = W.Sync.GetConflicts<FeedSyncSyndicationItem>(formatter); 
				//3.3.11
				foreach (FeedSyncSyndicationItem z in M)
				{
					if (!z.Equals(W) && !conflictsW.Contains(z))
					{
						conflictsW.Add(z);
					}
				}

				//TODO: Serialize the conflicts and add them to the W.Sync.RawConflicts collection
			}

			if (conflictsW.Count > 0)
			{
				return MergeOperation.Conflict;
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
		private static void PerformStep7(ref List<FeedSyncSyndicationItem> outerCollection, ref List<FeedSyncSyndicationItem> innerCollection, List<FeedSyncSyndicationItem> M, ref FeedSyncSyndicationItem W)
		{
			List<FeedSyncSyndicationItem> resOuter = new List<FeedSyncSyndicationItem>(outerCollection);
			List<FeedSyncSyndicationItem> resInner = new List<FeedSyncSyndicationItem>(innerCollection);

			// Collections must be modified from this method.
			foreach (FeedSyncSyndicationItem x in outerCollection)
			{
				bool isSubsumed = false;
				foreach (FeedSyncSyndicationItem y in innerCollection)
				{
					if (x.Sync.IsSubsumedBy(y))
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
		///The �winning� item between an item X and an item Y is the item with most recent update , where X and Y are items with the same id attribute value.  In order to determine the �winning� item, implementations MUST perform the following comparisons, in order, for X and Y:
		///1.	If X has a greater updates attribute value for the sx:sync sub-element than Y�s, then X is the �winning� item
		///2.	If X has the same updates attribute value for the sx:sync sub-element as Y:
		///a.	If X has a when attribute for the topmost sx:history sub-element and Y does not, then X is the �winning� item
		///b.	If X has a when attribute value for the topmost sx:history sub-element and that is chronologically later than Y�s, then X is the �winning� item
		///c.	If X has the same when attribute for the topmost sx:history sub-element as Y:
		///i.	If X has a by attribute for the topmost sx:history sub-element and Y does not, then X is the �winning� item 
		///ii.	If X has a by attribute value for the topmost sx:history sub-element that is collates greater (see Section 2.4 for collation rules) than Y�s, then X is the �winning� item
		///3.	Y is the �winning� item
		/// </summary>
		private static FeedSyncSyndicationItem WinnerPicking(FeedSyncSyndicationItem item, FeedSyncSyndicationItem anotherItem)
		{
			FeedSyncSyndicationItem winner = null;

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

		private static bool FirstWinsWithWhen(FeedSyncSyndicationItem first, FeedSyncSyndicationItem second, out FeedSyncSyndicationItem winner)
		{
			winner = null;

			if (first.Sync.LastUpdate.When == null) return false;

			bool firstWins = second.Sync.LastUpdate.When == null ||
					(first.Sync.LastUpdate.When > second.Sync.LastUpdate.When);

			if (firstWins) winner = first;

			return firstWins;
		}

		private static bool FirstWinsWithBy(FeedSyncSyndicationItem first, FeedSyncSyndicationItem second, out FeedSyncSyndicationItem winner)
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
