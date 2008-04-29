using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ServiceModel.Syndication;

namespace FeedSync
{
	/// <summary>
	/// A repository that splits its data between an <see cref="IXmlRepository"/> containing 
	/// the actual data, and an <see cref="ISyncRepository"/> containing the SSE metadata.
	/// </summary>
	[Obsolete("Use IRepository interface directly")]
	public partial class CompoundRepository : IRepository
	{
		IXmlRepository xmlRepo;
		ISyncRepository syncRepo;

		/// <summary>
		/// Default constructor
		/// </summary>
		public CompoundRepository()
		{
		}

		/// <summary>
		/// Initializes the repository with the two split repositories.
		/// </summary>
		/// <param name="xmlRepo">Repository for the actual entity data.</param>
		/// <param name="syncRepo">Repository for the SSE metadata.</param>
		/// <exception cref="ArgumentNullException"><paramref name="xmlRepo"/> or <paramref name="syncRepo"/> are null.</exception>
		public CompoundRepository(IXmlRepository xmlRepo, ISyncRepository syncRepo)
		{
			Guard.ArgumentNotNull(xmlRepo, "xmlRepo");
			Guard.ArgumentNotNull(syncRepo, "syncRepo");

			this.xmlRepo = xmlRepo;
			this.syncRepo = syncRepo;
			Initialize();

			Tracer.TraceData(this, TraceEventType.Information, "{0} / {1} initialized", xmlRepo.GetType().FullName,
				syncRepo.GetType().FullName);
		}

		/// <summary>
		/// Gets the repository for the actual entity data.
		/// </summary>
		public IXmlRepository XmlRepository 
		{ 
			get { return xmlRepo; }
			set { xmlRepo = value; RaiseXmlRepositoryChanged(); }
		}

		/// <summary>
		/// Gets the repository for the SSE metadata.
		/// </summary>
		public ISyncRepository SyncRepository 
		{ 
			get { return syncRepo; }
			set { syncRepo = value; RaiseSyncRepositoryChanged(); }
		}

		/// <summary>
		/// Returns <see langword="false"/> as this repository does not provide its own 
		/// merge behavior.
		/// </summary>
		public bool SupportsMerge
		{
			get { return false; }
		}

		/// <summary>
		/// See <see cref="IRepository.Get"/>.
		/// </summary>
		public FeedSyncSyndicationItem Get(string id)
		{
			Guard.ArgumentNotNullOrEmptyString(id, "id");

			EnsureInitialized();

			Tracer.TraceData(this, TraceEventType.Verbose, "Getting item with ID {0}", id);

			Sync sync = syncRepo.Get(id);
			XmlItem xml = xmlRepo.Get(id);
			
			if (xml == null && sync == null)
			{
				Tracer.TraceData(this, TraceEventType.Verbose, "No item found with ID {0}", id);
				return null;
			}

			AutoUpdateSync(ref xml, ref sync);
			if (xml == null)
				return new FeedSyncSyndicationItem(sync);
			else
				return new FeedSyncSyndicationItem(xml.Title, xml.Description, xml.Payload, sync);
		}

		/// <summary>
		/// See <see cref="IRepository.GetAll"/>.
		/// </summary>
		public IEnumerable<FeedSyncSyndicationItem> GetAll()
		{
			Tracer.TraceData(this, TraceEventType.Verbose, "Getting all items");

			return GetAllImpl(null, NullFilter);
		}

		/// <summary>
		/// See <see cref="IRepository.GetAll(Predicate{Item})"/>.
		/// </summary>
		public IEnumerable<FeedSyncSyndicationItem> GetAll(Predicate<FeedSyncSyndicationItem> filter)
		{
			Guard.ArgumentNotNull(filter, "filter");

			Tracer.TraceData(this, TraceEventType.Verbose, "Getting all items with filter {0}", filter);

			return GetAllImpl(null, filter);
		}

		/// <summary>
		/// See <see cref="IRepository.GetAllSince"/>.
		/// </summary>
		public IEnumerable<FeedSyncSyndicationItem> GetAllSince(DateTime? since)
		{
			Tracer.TraceData(this, TraceEventType.Verbose, "Getting all items since {0}", since);

			return GetAllImpl(since, NullFilter);
		}

		/// <summary>
		/// See <see cref="IRepository.GetAllSince(DateTime?, Predicate{Item})"/>.
		/// </summary>
		public IEnumerable<FeedSyncSyndicationItem> GetAllSince(DateTime? since, Predicate<FeedSyncSyndicationItem> filter)
		{
			Tracer.TraceData(this, TraceEventType.Verbose, "Getting all items since {0} with filter {1}", since, filter);

			return GetAllImpl(since, filter);
		}

		private static bool NullFilter(FeedSyncSyndicationItem item)
		{
			return true;
		}

		private IEnumerable<FeedSyncSyndicationItem> GetAllImpl(DateTime? since, Predicate<FeedSyncSyndicationItem> filter)
		{
			Guard.ArgumentNotNull(filter, "filter");

			EnsureInitialized();

			// Search deleted items.
			// TODO: Is there a better way than iterating every sync?
			// Note that we're only iterating all the Sync elements, which 
			// means we're not actually re-hidrating all entities just 
			// to find the deleted ones.
			IEnumerator<Sync> syncEnum = syncRepo.GetAll().GetEnumerator();

			IEnumerable<XmlItem> items = since.HasValue ?
				xmlRepo.GetAllSince(since.Value) :
				xmlRepo.GetAll();

			if (!since.HasValue)
			{
				since = DateTime.MinValue;
			}
			else
			{
				since = Timestamp.Normalize(since.Value);
			}

			foreach (XmlItem xmlItem in items)
			{
				Tracer.TraceData(this, TraceEventType.Verbose, "Getting sync info for item with ID {0}", xmlItem.Id);

				Sync sync = syncRepo.Get(xmlItem.Id);
				XmlItem xml = xmlItem;

				AutoUpdateSync(ref xml, ref sync);

				// Process deleted items mixed with regular 
				// items, so that we don't take as much time
				// at the end of the item building process.
				// Hopefully, both should finish about the same time.

				if (HasChangedSince(since.Value, sync))
				{
					Tracer.TraceData(this, TraceEventType.Verbose, "Item with ID {0} has changed since {1}", xmlItem.Id, since);

					FeedSyncSyndicationItem item = null;
					if (xml == null)
						item = new FeedSyncSyndicationItem(sync);
					else
						item = new FeedSyncSyndicationItem(xml.Title, xml.Description, xml.Payload, sync);
					
					if (filter(item))
						yield return item;
				}

				if (syncEnum.MoveNext())
				{
					// Is the item deleted from the XML repo?
					if (!xmlRepo.Contains(syncEnum.Current.Id))
					{
						Tracer.TraceData(this, TraceEventType.Verbose, "Item with ID {0} not found in Xml Repository", syncEnum.Current.Id);

						sync = syncEnum.Current;

						// Item does not exist in the XML repo, but the sync is not marked 
						// as deleted, so we need to update it now on-the-fly
						if (!syncEnum.Current.Deleted)
						{
							sync = syncEnum.Current.Delete(DeviceAuthor.Current, DateTime.Now);
							syncRepo.Save(sync);

							Tracer.TraceData(this, TraceEventType.Verbose, "Item with ID {0} marked as deleted", syncEnum.Current.Id);
						}

						if (HasChangedSince(since.Value, sync))
						{
							Tracer.TraceData(this, TraceEventType.Verbose, "Null Item with ID {0} has changed since {1}", syncEnum.Current.Id, since);

							FeedSyncSyndicationItem item = new FeedSyncSyndicationItem(sync);
							if (filter(item))
								yield return item;
						}
					}
				}
			}

			// If there are remaining items in sync, 
			// keep processing 'till the end.
			while (syncEnum.MoveNext())
			{
				// Is the item deleted from the XML repo?
				if (!xmlRepo.Contains(syncEnum.Current.Id))
				{
					Tracer.TraceData(this, TraceEventType.Verbose, "Item with ID {0} not found in Xml Repository", syncEnum.Current.Id);

					Sync sync = syncEnum.Current;
					
					// Item does not exist in the XML repo, but the sync is not marked 
					// as deleted, so we need to update it now on-the-fly
					if (!syncEnum.Current.Deleted)
					{
						sync = syncEnum.Current.Delete(DeviceAuthor.Current, DateTime.Now);
						syncRepo.Save(sync);

						Tracer.TraceData(this, TraceEventType.Verbose, "Item with ID {0} marked as deleted", syncEnum.Current.Id);
					}

					if (HasChangedSince(since.Value, sync))
					{
						Tracer.TraceData(this, TraceEventType.Verbose, "Null Item with ID {0} has changed since {1}", syncEnum.Current.Id, since);

						FeedSyncSyndicationItem item = new FeedSyncSyndicationItem(sync);
						if (filter(item))
							yield return item;
					}
				}
			}
		}

		/// <summary>
		/// See <see cref="IRepository.GetConflicts"/>.
		/// </summary>
		public IEnumerable<FeedSyncSyndicationItem> GetConflicts()
		{
			EnsureInitialized();

			Tracer.TraceData(this, TraceEventType.Verbose, "Getting all conflicts");

			foreach (Sync sync in syncRepo.GetConflicts())
			{
				XmlItem item = xmlRepo.Get(sync.Id);
				Sync itemSync = sync;
				if (item == null)
				{
					Tracer.TraceData(this, TraceEventType.Verbose, "Conflict with ID {0} not found in Xml Repository", sync.Id);

					// Update deletion if necessary.
					if (!sync.Deleted)
					{
						itemSync = sync.Delete(DeviceAuthor.Current, DateTime.Now);
						syncRepo.Save(itemSync);

						Tracer.TraceData(this, TraceEventType.Verbose, "Conflict with ID {0} marked as deleted", sync.Id);
					}
				}
				else
				{
					itemSync = UpdateSyncIfItemHashChanged(item, sync);
				}

				yield return new FeedSyncSyndicationItem(item.Title, item.Description, item.Payload, itemSync);
			}
		}


		/// <summary>
		/// See <see cref="IRepository.Add"/>.
		/// </summary>
		public void Add(FeedSyncSyndicationItem item)
		{
			Guard.ArgumentNotNull(item, "item");

			EnsureInitialized();

			Tracer.TraceData(this, TraceEventType.Verbose, "Adding item with ID {0}", item.Sync.Id);

			object tag = null;
			if (!item.Sync.Deleted)
			{
				xmlRepo.Add(new XmlItem(item.Title.Text, item.Summary.Text,
					item.Content), out tag);

				Tracer.TraceData(this, TraceEventType.Verbose, "Item with ID {0} added to Xml Repository", item.Sync.Id);
			}

			item.Sync.Tag = tag;
			syncRepo.Save(item.Sync);

			Tracer.TraceData(this, TraceEventType.Verbose, "Item with ID {0} added to Sync Repository with Hash {1}", item.Sync.Id, item.Sync.Tag);
		}

		/// <summary>
		/// See <see cref="IRepository.Delete"/>.
		/// </summary>
		public void Delete(string id)
		{
			Guard.ArgumentNotNullOrEmptyString(id, "id");

			EnsureInitialized();

			Tracer.TraceData(this, TraceEventType.Verbose, "Deleting item with ID {0}", id);

			xmlRepo.Remove(id);
			
			Tracer.TraceData(this, TraceEventType.Verbose, "Item with ID {0} deleted from Xml Repository", id);

			Sync sync = syncRepo.Get(id);
			if (sync != null)
			{
				sync = sync.Delete(DeviceAuthor.Current, DateTime.Now);
				syncRepo.Save(sync);

				Tracer.TraceData(this, TraceEventType.Verbose, "Item with ID {0} deleted from Sync Repository", id);
			}
		}

		/// <summary>
		/// See <see cref="IRepository.Update"/>.
		/// </summary>
		public void Update(FeedSyncSyndicationItem item)
		{
			Guard.ArgumentNotNull(item, "item");

			EnsureInitialized();

			Tracer.TraceData(this, TraceEventType.Verbose, "Updating item with ID {0}", item.Sync.Id);

			if (item.Sync.Deleted)
			{
				xmlRepo.Remove(item.Sync.Id);

				Tracer.TraceData(this, TraceEventType.Verbose, "Item with ID {0} deleted from Xml Repository", item.Sync.Id);
			}
			else
			{
				object tag = null;

				xmlRepo.Update(new XmlItem(item.Title.Text, item.Summary.Text, item.Content), out tag);
				item.Sync.Tag = tag;

				Tracer.TraceData(this, TraceEventType.Verbose, "Item with ID {0} updated in Sync Repository with Tag {1}", item.Sync.Id, item.Sync.Tag);
			}

			syncRepo.Save(item.Sync);
		}

		/// <summary>
		/// See <see cref="IRepository.Update(Item, bool)"/>.
		/// </summary>
		public FeedSyncSyndicationItem Update(FeedSyncSyndicationItem item, bool resolveConflicts)
		{
			Guard.ArgumentNotNull(item, "item");

			EnsureInitialized();

			Tracer.TraceData(this, TraceEventType.Verbose, "Updating item with ID {0} before resolving conflicts", item.Sync.Id);

			if (resolveConflicts)
			{
				// TODO: verify if this is what we were doing before.
				item = item.ResolveConflicts(DeviceAuthor.Current, DateTime.Now,
					item.Sync.Deleted);
			}

			Update(item);
            return item;
		}

		/// <summary>
		/// Throws <see cref="NotSupportedException"/> as the merge behavior 
		/// is not provided by this repository.
		/// </summary>
		/// <exception cref="NotSupportedException">Thrown always.</exception>
		public IEnumerable<FeedSyncSyndicationItem> Merge(IEnumerable<FeedSyncSyndicationItem> items)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// See <see cref="IRepository.FriendlyName"/>.
		/// </summary>
		public virtual string FriendlyName
		{
			// TODO: return friendly name of xml repo (add to IXmlRepository interface)
			get { return this.GetType().Name; }
		}

		private void AutoUpdateSync(ref XmlItem xml, ref Sync sync)
		{
			if (xml != null && sync == null)
			{
				// Add sync on-the-fly.
				sync = Sync.Create(xml.Id, DeviceAuthor.Current, DateTime.Now);
				sync.Tag = xml.Tag;
				syncRepo.Save(sync);

				Tracer.TraceData(this, TraceEventType.Verbose, "Created sync for item with ID {0} and hashcode {1}", xml.Id, sync.Tag);
			}
			else if (xml == null && sync != null)
			{
				if (!sync.Deleted)
				{
					sync = sync.Delete(DeviceAuthor.Current, DateTime.Now);
					syncRepo.Save(sync);

					Tracer.TraceData(this, TraceEventType.Verbose, "Item with ID {0} marked as deleted", sync.Id);
				}

				xml = null;
			}
			else
			{
				sync = UpdateSyncIfItemHashChanged(xml, sync);
			}
		}

		/// <summary>
		/// Ensures the Sync information is current WRT the 
		/// item actual data. If it's not, a new 
		/// update will be added. Used when exporting/retrieving 
		/// items from the local stores.
		/// </summary>
		private Sync UpdateSyncIfItemHashChanged(XmlItem item, Sync sync)
		{
			// TODO: check if this is correct.
			if (item.Tag.ToString() != sync.Tag.ToString())
			{
				Tracer.TraceData(this, TraceEventType.Verbose, "Updating Sync for Item with ID {0} - Original Tag {1} / Current Tag {2}", sync.Id, sync.Tag, item.Tag);

				Sync updated = null;
				if (sync.Deleted)
					updated = sync.Update(DeviceAuthor.Current, DateTime.Now);
				else
					updated = sync.Update(DeviceAuthor.Current, DateTime.Now);

				sync.Tag = item.Tag;
				syncRepo.Save(sync);

				return updated;
			}
			
			return sync;
		}

		private static bool HasChangedSince(DateTime since, Sync sync)
		{
			// Item without a when is always returned.
			if (sync.LastUpdate == null ||
				sync.LastUpdate.When == null)
				return true;
			else
				return sync.LastUpdate.When.Value >= since;
		}

		//private void TraceData(TraceEventType severity, 

		// TODO: XamlBinding - Implement instance validation here
		private void DoValidate()
		{
			if (xmlRepo == null)
				throw new ArgumentNullException("XmlRepository", Properties.Resources.UnitializedXmlRepository);

			if (syncRepo == null)
				throw new ArgumentNullException("SyncRepository", Properties.Resources.UnitializedSyncRepository);
		}

		// TODO: XamlBinding - Implement initialization here
		protected virtual void DoInitialize()
		{

		}
	}
}