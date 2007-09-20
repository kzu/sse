using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSharing
{
	/// <summary>
	/// Main class that performs synchronization between two repositories.
	/// </summary>
	public class SyncEngine
	{
		IRepository left;
		IRepository right;

		/// <summary>
		/// Initializes the engine with the two repositories to synchronize.
		/// </summary>
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

		/// <summary>
		/// Performs a full sync between the two repositories, automatically 
		/// incorporating changes in both.
		/// </summary>
		/// <returns>The list of items that had conflicts.</returns>
		public IList<Item> Synchronize()
		{
			return SynchronizeImpl(null, NullPreviewHandler, PreviewBehavior.None);
		}

		/// <summary>
		/// Performs a full sync between the two repositories, optionally calling the 
		/// given <paramref name="previewer"/> callback as specified by the <paramref name="behavior"/> argument.
		/// </summary>
		/// <returns>The list of items that had conflicts.</returns>
		public IList<Item> Synchronize(PreviewImportHandler previewer, PreviewBehavior behavior)
		{
			return SynchronizeImpl(null, previewer, behavior);
		}

		/// <summary>
		/// Performs a partial sync between the two repositories since the specified date, automatically 
		/// incorporating changes in both.
		/// </summary>
		/// <param name="since">Synchronize changes that happened after this date.</param>
		/// <returns>The list of items that had conflicts.</returns>
		public IList<Item> Synchronize(DateTime? since)
		{
			return SynchronizeImpl(since, NullPreviewHandler, PreviewBehavior.None);
		}

		/// <summary>
		/// Performs a partial sync between the two repositories since the specified date, optionally calling the 
		/// given <paramref name="previewer"/> callback as specified by the <paramref name="behavior"/> argument.
		/// </summary>
		/// <param name="since">Synchronize changes that happened after this date.</param>
		/// <returns>The list of items that had conflicts.</returns>
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
}
