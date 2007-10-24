using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSharing
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
		public Item Get(string id)
		{
			Guard.ArgumentNotNullOrEmptyString(id, "id");

			EnsureInitialized();

			Sync sync = syncRepo.Get(id);
			IXmlItem xml = xmlRepo.Get(id);

			if (xml == null && sync == null)
			{
				return null;
			}

			AutoUpdateSync(ref xml, ref sync);

			return new Item(xml, sync);
		}

		/// <summary>
		/// See <see cref="IRepository.GetAll"/>.
		/// </summary>
		public IEnumerable<Item> GetAll()
		{
			return GetAllImpl(null, NullFilter);
		}

		/// <summary>
		/// See <see cref="IRepository.GetAll(Predicate{Item})"/>.
		/// </summary>
		public IEnumerable<Item> GetAll(Predicate<Item> filter)
		{
			Guard.ArgumentNotNull(filter, "filter");

			return GetAllImpl(null, filter);
		}

		/// <summary>
		/// See <see cref="IRepository.GetAllSince"/>.
		/// </summary>
		public IEnumerable<Item> GetAllSince(DateTime? since)
		{
			return GetAllImpl(since, NullFilter);
		}

		/// <summary>
		/// See <see cref="IRepository.GetAllSince(DateTime?, Predicate{Item})"/>.
		/// </summary>
		public IEnumerable<Item> GetAllSince(DateTime? since, Predicate<Item> filter)
		{
			return GetAllImpl(since, filter);
		}

		private static bool NullFilter(Item item)
		{
			return true;
		}

		private IEnumerable<Item> GetAllImpl(DateTime? since, Predicate<Item> filter)
		{
			Guard.ArgumentNotNull(filter, "filter");

			EnsureInitialized();

			// Search deleted items.
			// TODO: Is there a better way than iterating every sync?
			// Note that we're only iterating all the Sync elements, which 
			// means we're not actually re-hidrating all entities just 
			// to find the deleted ones.
			IEnumerator<Sync> syncEnum = syncRepo.GetAll().GetEnumerator();

			IEnumerable<IXmlItem> items = since.HasValue ?
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

			foreach (IXmlItem xmlItem in items)
			{
				Sync sync = syncRepo.Get(xmlItem.Id);
				IXmlItem xml = xmlItem;

				AutoUpdateSync(ref xml, ref sync);

				// Process deleted items mixed with regular 
				// items, so that we don't take as much time
				// at the end of the item building process.
				// Hopefully, both should finish about the same time.

				if (HasChangedSince(since.Value, sync))
				{
					Item item = new Item(xml, sync);
					if (filter(item))
						yield return item;
				}

				if (syncEnum.MoveNext())
				{
					// Is the item deleted from the XML repo?
					if (!xmlRepo.Contains(syncEnum.Current.Id))
					{
						sync = syncEnum.Current;

						// Item does not exist in the XML repo, but the sync is not marked 
						// as deleted, so we need to update it now on-the-fly
						if (!syncEnum.Current.Deleted)
						{
							sync = Behaviors.Update(syncEnum.Current, DeviceAuthor.Current, DateTime.Now, true);
							syncRepo.Save(sync);
						}

						if (HasChangedSince(since.Value, sync))
						{
							Item item = new Item(new NullXmlItem(syncEnum.Current.Id), sync);
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
					Sync sync = syncEnum.Current;

					// Item does not exist in the XML repo, but the sync is not marked 
					// as deleted, so we need to update it now on-the-fly
					if (!syncEnum.Current.Deleted)
					{
						sync = Behaviors.Update(syncEnum.Current, DeviceAuthor.Current, DateTime.Now, true);
						syncRepo.Save(sync);
					}

					if (HasChangedSince(since.Value, sync))
					{
						Item item = new Item(new NullXmlItem(syncEnum.Current.Id), sync);
						if (filter(item))
							yield return item;
					}
				}
			}
		}

		/// <summary>
		/// See <see cref="IRepository.GetConflicts"/>.
		/// </summary>
		public IEnumerable<Item> GetConflicts()
		{
			EnsureInitialized();

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


		/// <summary>
		/// See <see cref="IRepository.Add"/>.
		/// </summary>
		public void Add(Item item)
		{
			Guard.ArgumentNotNull(item, "item");

			EnsureInitialized();

			if (!item.Sync.Deleted)
			{
				xmlRepo.Add(item.XmlItem);
			}

			// TODO: replace with hash property.
			item.Sync.ItemHash = item.XmlItem.GetHashCode(); 
			syncRepo.Save(item.Sync);
		}

		/// <summary>
		/// See <see cref="IRepository.Delete"/>.
		/// </summary>
		public void Delete(string id)
		{
			Guard.ArgumentNotNullOrEmptyString(id, "id");

			EnsureInitialized();

			xmlRepo.Remove(id);
			Sync sync = syncRepo.Get(id);
			if (sync != null)
			{
				sync = Behaviors.Delete(sync, DeviceAuthor.Current, DateTime.Now);
				syncRepo.Save(sync);
			}
		}

		/// <summary>
		/// See <see cref="IRepository.Update"/>.
		/// </summary>
		public void Update(Item item)
		{
			Guard.ArgumentNotNull(item, "item");

			EnsureInitialized();

			if (item.Sync.Deleted)
			{
				xmlRepo.Remove(item.Sync.Id);
			}
			else
			{
				// TODO: Should return the hash and save it to the sync.ItemHash.
				xmlRepo.Update(item.XmlItem);
				item.Sync.ItemHash = item.XmlItem.GetHashCode();
			}

			syncRepo.Save(item.Sync);
		}

		/// <summary>
		/// See <see cref="IRepository.Update(Item, bool)"/>.
		/// </summary>
		public void Update(Item item, bool resolveConflicts)
		{
			Guard.ArgumentNotNull(item, "item");

			EnsureInitialized();

			if (resolveConflicts)
			{
				// TODO: verify if this is what we were doing before.
				item = Behaviors.ResolveConflicts(item, DeviceAuthor.Current, DateTime.Now,
					item.Sync.Deleted);
			}

			Update(item);
		}

		/// <summary>
		/// Throws <see cref="NotSupportedException"/> as the merge behavior 
		/// is not provided by this repository.
		/// </summary>
		/// <exception cref="NotSupportedException">Thrown always.</exception>
		public IEnumerable<Item> Merge(IEnumerable<Item> items)
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

		private void AutoUpdateSync(ref IXmlItem xml, ref Sync sync)
		{
			if (xml != null && sync == null)
			{
				// Add sync on-the-fly.
				sync = Behaviors.Create(xml.Id, DeviceAuthor.Current, DateTime.Now, false);
				sync.ItemHash = xml.GetHashCode();
				syncRepo.Save(sync);
			}
			else if (xml == null && sync != null)
			{
				if (!sync.Deleted)
				{
					sync = Behaviors.Delete(sync, DeviceAuthor.Current, DateTime.Now);
					syncRepo.Save(sync);
				}

				xml = new NullXmlItem(sync.Id);
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
		private Sync UpdateSyncIfItemHashChanged(IXmlItem item, Sync sync)
		{
			if (!item.GetHashCode().Equals(sync.ItemHash))
			{
				Sync updated = Behaviors.Update(sync,
					DeviceAuthor.Current,
					DateTime.Now, false);
				sync.ItemHash = item.GetHashCode();
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
