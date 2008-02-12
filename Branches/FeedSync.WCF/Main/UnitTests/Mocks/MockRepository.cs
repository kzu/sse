using System;
using System.Collections.Generic;
using System.Text;

namespace FeedSync.Tests
{
	public class MockRepository : Repository
	{
		public string name;
		public Dictionary<string, FeedSyncSyndicationItem> Items = new Dictionary<string, FeedSyncSyndicationItem>();

		public MockRepository(params FeedSyncSyndicationItem[] items)
		{
			foreach (FeedSyncSyndicationItem item in items)
			{
				Items.Add(item.Sync.Id, item);
			}
		}

		public MockRepository(string name)
		{
			this.name = name;
		}

		public override string FriendlyName
		{
			get { return name; }
		}

		public override bool SupportsMerge
		{
			get { return false; }
		}

		public override void Add(FeedSyncSyndicationItem item)
		{
			Guard.ArgumentNotNull(item, "item");

			if (Items.ContainsKey(item.Sync.Id))
				throw new ArgumentException();

			Items.Add(item.Sync.Id, item);
		}

		public override FeedSyncSyndicationItem Get(string id)
		{
			Guard.ArgumentNotNullOrEmptyString(id, "id");

			if (Items.ContainsKey(id))
				return new FeedSyncSyndicationItem(Items[id]);
			else
				return null;
		}

		protected override IEnumerable<FeedSyncSyndicationItem> GetAll(DateTime? since, Predicate<FeedSyncSyndicationItem> filter)
		{
			Guard.ArgumentNotNull(filter, "filter");

			foreach (FeedSyncSyndicationItem i in Items.Values)
			{
				if ((since == null ||
					i.Sync.LastUpdate == null ||
					i.Sync.LastUpdate.When == null ||
					i.Sync.LastUpdate.When >= since)
					&& filter(i))
					yield return new FeedSyncSyndicationItem(i);
			}
		}

		public override void Delete(string id)
		{
			Guard.ArgumentNotNullOrEmptyString(id, "id");

			Items.Remove(id);
		}

		public override void Update(FeedSyncSyndicationItem item)
		{
			Guard.ArgumentNotNull(item, "item");

			FeedSyncSyndicationItem i;
			if (item.Sync.Deleted)
				i = new FeedSyncSyndicationItem(item.Sync.Clone());
			else
				i = new FeedSyncSyndicationItem(item);

			Items[item.Sync.Id] = i;
		}

		public override IEnumerable<FeedSyncSyndicationItem> Merge(IEnumerable<FeedSyncSyndicationItem> items)
		{
			throw new NotSupportedException();
		}
	}
}
