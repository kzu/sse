using System;
using System.Collections.Generic;

namespace SimpleSharing.Adapters.Memory
{
	public class MemoryRepository : Repository
	{
		Dictionary<string, Item> items = new Dictionary<string, Item>();

		public override bool SupportsMerge
		{
			get { return false; }
		}

		public override Item Get(string id)
		{
			Item item = null;
			if (items.TryGetValue(id, out item))
			{
				item = item.Clone();
			}

			return item;
		}

		public override IEnumerable<Item> GetAllSince(DateTime? since, Predicate<Item> filter)
		{
			foreach (Item item in items.Values)
			{
				if (since == null || item.Sync.LastUpdate == null || item.Sync.LastUpdate.When == null)
					yield return item;
				else if (item.Sync.LastUpdate.When >= since)
					yield return item;
			}
		}

		public override void Add(Item item)
		{
			if (items.ContainsKey(item.Sync.Id))
				throw new InvalidOperationException("duplicate item id");

			items.Add(item.Sync.Id, item.Clone());
		}

		public override void Delete(string id)
		{
			items.Remove(id);
		}

		public override void Update(Item item)
		{
			items[item.Sync.Id] = item.Clone();
		}

		public override IEnumerable<Item> Merge(IEnumerable<Item> items)
		{
			throw new NotSupportedException();
		}

		public override string FriendlyName
		{
			get { return "Memory Adapter"; }
		}
	}
}
