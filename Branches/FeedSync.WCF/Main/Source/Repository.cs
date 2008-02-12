using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace FeedSync
{
	/// <summary>
	/// Base implementation of <see cref="IRepository"/> that provides support for 
	/// <see cref="ISupportInitialize"/> for XAML-friendly serialization and validation.
	/// </summary>
	public abstract class Repository : IRepository
	{
		/// <summary>
		/// See <see cref="IRepository.SupportsMerge"/>.
		/// </summary>
		public abstract bool SupportsMerge { get; }

		/// <summary>
		/// See <see cref="IRepository.Get"/>.
		/// </summary>
		public abstract FeedSyncSyndicationItem Get(string id);

		private static bool NullFilter(FeedSyncSyndicationItem item)
		{
			return true;
		}

		/// <summary>
		/// See <see cref="IRepository.GetAll"/>.
		/// </summary>
		public IEnumerable<FeedSyncSyndicationItem> GetAll()
		{
			return GetAllSince(null, NullFilter);
		}

		/// <summary>
		/// See <see cref="IRepository.GetAll(Predicate{Item})"/>.
		/// </summary>
		public IEnumerable<FeedSyncSyndicationItem> GetAll(Predicate<FeedSyncSyndicationItem> filter)
		{
			return GetAllSince(null, filter);
		}

		/// <summary>
		/// See <see cref="IRepository.GetAllSince(DateTime?)"/>.
		/// </summary>
		public IEnumerable<FeedSyncSyndicationItem> GetAllSince(DateTime? since)
		{
			return GetAllSince(since, NullFilter);
		}

		/// <summary>
		/// See <see cref="IRepository.GetAllSince(DateTime?, Predicate{Item})"/>.
		/// </summary>
		public IEnumerable<FeedSyncSyndicationItem> GetAllSince(DateTime? since, Predicate<FeedSyncSyndicationItem> filter)
		{
			return GetAll(since == null ? since : Timestamp.Normalize(since.Value), filter);
		}

		protected abstract IEnumerable<FeedSyncSyndicationItem> GetAll(DateTime? since, Predicate<FeedSyncSyndicationItem> filter);

		/// <summary>
		/// See <see cref="IRepository.GetConflicts"/>. Default implementation retrieves 
		/// all items and filters out those without conflicts.
		/// </summary>
		public virtual IEnumerable<FeedSyncSyndicationItem> GetConflicts()
		{
			return GetAllSince(null, delegate(FeedSyncSyndicationItem item)
			{
				return item.Sync.Conflicts.Count > 0;
			});
		}

		/// <summary>
		/// See <see cref="IRepository.Add"/>.
		/// </summary>
		public abstract void Add(FeedSyncSyndicationItem item);

		/// <summary>
		/// See <see cref="IRepository.Delete"/>.
		/// </summary>
		public abstract void Delete(string id);

		/// <summary>
		/// See <see cref="IRepository.Update"/>.
		/// </summary>
		public abstract void Update(FeedSyncSyndicationItem item);

		/// <summary>
		/// See <see cref="IRepository.Update(Item, bool)"/>. Default implementation 
		/// uses <see cref="Behaviors.ResolveConflicts"/> to generate a new update 
		/// that resolves all conflicts, with the <see cref="DeviceAuthor.Current"/> and 
		/// <see cref="DateTime.Now"/> as the by/when information.
		/// </summary>
		public virtual FeedSyncSyndicationItem Update(FeedSyncSyndicationItem item, bool resolveConflicts)
		{
			Guard.ArgumentNotNull(item, "item");

			if (resolveConflicts)
			{
				item = item.ResolveConflicts(DeviceAuthor.Current, DateTime.Now, item.Sync.Deleted);
			}
			
			Update(item);

            return item;
		}

		/// <summary>
		/// See <see cref="IRepository.Merge"/>.
		/// </summary>
		public abstract IEnumerable<FeedSyncSyndicationItem> Merge(IEnumerable<FeedSyncSyndicationItem> items);

		/// <summary>
		/// See <see cref="IRepository.FriendlyName"/>.
		/// </summary>
		public abstract string FriendlyName { get; }
	}
}
