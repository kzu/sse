using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;

namespace SimpleSharing
{
	public class SyncEngine
	{
		IXmlRepository xmlRepo;
		ISyncRepository syncRepo;

		public SyncEngine(
			IXmlRepository xmlRepository,
			ISyncRepository syncRepository)
		{
			Guard.ArgumentNotNull(xmlRepository, "xmlRepository");
			Guard.ArgumentNotNull(syncRepository, "syncRepository");

			this.xmlRepo = xmlRepository;
			this.syncRepo = syncRepository;
		}

		public IEnumerable<Item> Export()
		{
			return BuildItems(xmlRepo.GetAll(), DateTime.MinValue);
		}

		public IEnumerable<Item> Export(DateTime? since)
		{
			if (since.HasValue)
			{
				return BuildItems(xmlRepo.GetAllSince(since.Value), since.Value);
			}
			else
			{
				return Export();
			}
		}

		public IEnumerable<Item> Export(int days)
		{
			if (days == -1)
			{
				return Export();
			}
			else
			{
				DateTime since = DateTime.Today.Subtract(TimeSpan.FromDays(days));
				return Export(since);
			}
		}

		private IEnumerable<Item> BuildItems(IEnumerable<IXmlItem> xmlItems, DateTime since)
		{
			// Search deleted items.
			// TODO: Is there a better way than iterating every sync?
			// Note that we're only iterating all the Sync elements, which 
			// means we're not actually re-hidrating all entities just 
			// to find the deleted ones.
			IEnumerator<Sync> syncEnum = syncRepo.GetAll().GetEnumerator();

			foreach (IXmlItem xml in xmlItems)
			{
				Sync sync = syncRepo.Get(xml.Id);

				if (sync == null)
				{
					// Add sync on-the-fly.
					sync = Behaviors.Create(xml.Id, DeviceAuthor.Current, DateTime.Now, false);
					syncRepo.Save(sync);
				}
				else
				{
					sync = UpdateSyncIfItemHashChanged(xml, sync);
				}

				if (HasChangedSince(since, xml, sync))
					yield return new Item(xml, sync);

				// Process deleted items mixed with regular 
				// items, so that we don't take as much time
				// at the end of the item building process.
				// Hopefully, both should finish about the same time.

				if (syncEnum.MoveNext())
				{
					if (!xmlRepo.Contains(syncEnum.Current.Id) && !syncEnum.Current.Deleted)
					{
						Sync updatedSync = Behaviors.Update(syncEnum.Current, DeviceAuthor.Current, DateTime.Now, true);
						syncRepo.Save(updatedSync);

						yield return new Item(null, updatedSync);
					}
				}
			}

			// If there are remaining items in sync, 
			// keep processing 'till the end.
			while (syncEnum.MoveNext())
			{
				if (!xmlRepo.Contains(syncEnum.Current.Id) && !syncEnum.Current.Deleted)
				{
					Sync updatedSync = Behaviors.Update(syncEnum.Current, DeviceAuthor.Current, DateTime.Now, true);
					syncRepo.Save(updatedSync);

					yield return new Item(null, updatedSync);
				}
			}
		}

		private static bool HasChangedSince(DateTime since, IXmlItem xml, Sync sync)
		{
			return sync.LastUpdate.When.Value >= since;
		}

		public IEnumerable<Item> ExportConflicts()
		{
			foreach (Sync sync in syncRepo.GetConflicts())
			{
				IXmlItem item = xmlRepo.Get(sync.Id);
				Sync itemSync = sync;
				if (item == null)
				{
					// Update deletion if necessary.
					if (!sync.Deleted)
					{
						itemSync = Behaviors.Update(sync, DeviceAuthor.Current, DateTime.Now, true);
						syncRepo.Save(itemSync);
					}
				}
				else
				{
					itemSync = UpdateSyncIfItemHashChanged(item, sync);
				}
				
				yield return new Item(item, itemSync);
			}
		}

		public IEnumerable<ItemMergeResult> PreviewImport(IEnumerable<Item> items)
		{
			foreach (Item incoming in items)
			{
				yield return Behaviors.Merge(xmlRepo, syncRepo, incoming);
			}
		}

		[Obsolete("Use Import overloads that do not receive a feedUrl argument.", true)]
		public IList<Item> Import(string feedUrl, IEnumerable<Item> items)
		{
			return Import(PreviewImport(items));
		}

		[Obsolete("Use Import overloads that do not receive a feedUrl argument.", true)]
		public IList<Item> Import(string feedUrl, params Item[] items)
		{
			return Import(PreviewImport(items));
		}

		[Obsolete("Use Import overloads that do not receive a feedUrl argument.", true)]
		public IList<Item> Import(string feedUrl, IEnumerable<ItemMergeResult> items)
		{
			return Import(items);
		}

		public IList<Item> Import(IEnumerable<Item> items)
		{
			return Import(PreviewImport(items));
		}

		public IList<Item> Import(params Item[] items)
		{
			return Import(PreviewImport(items));
		}

		public IList<Item> Import(IEnumerable<ItemMergeResult> items)
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
						if (!result.Proposed.Sync.Deleted)
						{
							xmlRepo.Add(result.Proposed.XmlItem);
                            result.Proposed.Sync.ItemHash = result.Proposed.XmlItem.GetHashCode();
							syncRepo.Save(result.Proposed.Sync);
						}
						break;
					case MergeOperation.Deleted:
						xmlRepo.Remove(result.Proposed.XmlItem.Id);
						result.Proposed.Sync.ItemHash = null;
						syncRepo.Save(result.Proposed.Sync);
						break;
					case MergeOperation.Updated:
					case MergeOperation.Conflict:
						// TODO: if there's a conflict but the winner is a 
						// delete, should we delete from the xmlRepo?
						if (!result.Proposed.Sync.Deleted)
						{
							xmlRepo.Update(result.Proposed.XmlItem);
                            result.Proposed.Sync.ItemHash = result.Proposed.XmlItem.GetHashCode();
						}
						else
						{
							result.Proposed.Sync.ItemHash = null;
						}
						syncRepo.Save(result.Proposed.Sync);
						break;
					case MergeOperation.None:
						break;
					default:
						throw new InvalidOperationException();
				}
			}

			return conflicts;
		}

		/// <summary>
		/// Manually saves or updates a new or existing item, 
		/// optionally merging the conflicts history.
		/// </summary>
		/// <remarks>
		/// See 3.4 on SSE spec.
		/// </remarks>
		/// <returns>The saved item and its new timestamp.</returns>
		public Item Save(Item item)
		{
			return Save(item, false);
		}

		public Item Save(Item item, bool resolveConflicts)
		{
			Guard.ArgumentNotNull(item, "item");

			if (resolveConflicts)
			{
				item = Behaviors.ResolveConflicts(item, DeviceAuthor.Current, DateTime.Now, false);
			}

			if (xmlRepo.Contains(item.XmlItem.Id))
			{
				xmlRepo.Update(item.XmlItem);
                item.Sync.ItemHash = item.XmlItem.GetHashCode();
			}
			else
			{
                xmlRepo.Add(item.XmlItem);
				item.Sync.ItemHash = item.XmlItem.GetHashCode();
			}
				
			syncRepo.Save(item.Sync);

			return item;
		}

		[Obsolete("Use Export and write to the FeedWriter directly")]
		public void Publish(Feed feed, FeedWriter writer)
		{
			// Since and Until are optional. We don't use them.
			IEnumerable<Item> items = Export();
			writer.Write(feed, items);
		}

		// Partial feed publishing
		[Obsolete("Use Export and write to the FeedWriter directly")]
		public void Publish(Feed feed, FeedWriter writer, int lastDays)
		{
			DateTime since = DateTime.Today.Subtract(TimeSpan.FromDays(lastDays));

			IEnumerable<Item> items = Export(lastDays);
			writer.Write(feed, items);
		}

		// TODO: Optimize subscribe when caller doesn't care about 
		// retrieving the conflicts.
		[Obsolete("Use FeedReader.Read and call Import with the resulting items directly")]
		public IList<Item> Subscribe(FeedReader reader)
		{
			Feed feed;
			IEnumerable<Item> items;

			// TODO: 5.1
			reader.Read(out feed, out items);

			return Import(feed.Link, items);
		}

		public DateTime? GetLastSync(string feed)
		{
			DateTime? lastSync = syncRepo.GetLastSync(feed);

			if (lastSync.HasValue)
			{
				lastSync = Timestamp.Normalize(lastSync.Value);
			}

			return lastSync;
		}

		public void SetLastSync(string feed, DateTime lastSync)
		{
			syncRepo.SetLastSync(feed, Timestamp.Normalize(lastSync));
		}

		/// <summary>
		/// Ensures the Sync information is current WRT the 
		/// item actual LastUpdated date. If it's not, a new 
		/// update will be added. Used when exporting/retrieving 
		/// items from the local stores.
		/// </summary>
		private Sync UpdateSyncIfItemHashChanged(IXmlItem item, Sync sync)
		{
			if (!item.GetHashCode().Equals(sync.ItemHash))
			{
				Sync updated = Behaviors.Update(sync,
					DeviceAuthor.Current,
					DateTime.Now, sync.Deleted);
				sync.ItemHash = item.GetHashCode();
				syncRepo.Save(sync);
				return updated;
			}

			return sync;
		}

        ///// <summary>
        ///// Ensures the LastUpdate property on the <see cref="IXmlItem"/> 
        ///// matches the Sync last update. This is the opposite of 
        ///// SynchronizeSyncFromItem, and is used for incoming items
        ///// being imported.
        ///// </summary>
        //private void UpdateItemHashIfSyncHasWhen(Item item)
        //{
        //    if (item != null &&
        //        item.XmlItem != null && 
        //        item.Sync.LastUpdate != null && 
        //        item.Sync.LastUpdate.When != null)
        //    {
        //        item.XmlItem.Hash = item.Sync.LastUpdate.When.Value; //TODO: What is the correct value here, Ask Kzu ?
        //    }
        //}
	}
}
