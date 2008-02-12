using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FeedSync
{
	public static class ResolveConflicsBehavior
	{
		public static FeedSyncSyndicationItem ResolveConflicts(this FeedSyncSyndicationItem resolvedItem, string by, DateTime? when, bool deleteItem)
		{
			//3.4	Conflict Resolution Behavior
			//Merging Conflict Items 
			//1.	Set R as a reference the resolved item
			//2.	Set Ry as a reference to the sx:sync sub-element for R
			//3.	For each item sub-element C of the sx:conflicts element that has been resolved:
			//	a.	Set Sc as a reference to the sx:sync sub-element for C
			//	b.	Remove C from the sx:conflicts element.
			//	b.	For each sx:history sub-element Hc of Sc:
			//		i.	For each sx:history sub-element Hr of Sr:
			//			aa.	Compare Hc with Hr to see if Hc can be subsumed2 by Hr – if so then process the next item sub-element
			//		ii.	Add Hr as a sub-element of Sr, immediately after the topmost sx:history sub-element of Sr.
			//3. If the sx:conflicts element contains no sub-elements, the sx:conflicts element SHOULD be removed.

			FeedSyncSyndicationItem R = (FeedSyncSyndicationItem)resolvedItem.Clone();
			Sync Sr = R.Sync;
			foreach (FeedSyncSyndicationItem C in Sr.Conflicts.ToArray())
			{
				Sync Sc = C.Sync;
				Sr.Conflicts.Remove(C);
				foreach (History Hc in Sc.UpdatesHistory)
				{
					bool isSubsumed = false;
					foreach (History Hr in Sr.UpdatesHistory)
					{
						if (Hc.IsSubsumedBy(Hr))
						{
							isSubsumed = true;
							break;
						}
					}
					if (isSubsumed)
					{
						break;
					}
					else
					{
						Sr.AddConflictHistory(Hc);
					}
				}
			}

			Sync updatedSync = null;
			if (deleteItem)
				updatedSync = Sr.Delete(by, when);
			else
				updatedSync = Sr.Update(by, when);

			return new FeedSyncSyndicationItem(R, updatedSync);
		}
	}
}
