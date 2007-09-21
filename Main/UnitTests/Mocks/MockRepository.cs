using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSharing.Tests
{
	public class MockRepository : Repository
	{
		public string name;
		public Dictionary<string, Item> Items = new Dictionary<string, Item>();

		public MockRepository(params Item[] items)
		{
			foreach (Item item in items)
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

		public override void Add(Item item)
		{
			Items.Add(item.Sync.Id, item);
		}

		public override Item Get(string id)
		{
			if (Items.ContainsKey(id))
				return Items[id].Clone();
			else
				return null;
		}

		public override IEnumerable<Item> GetAllSince(DateTime? since, Predicate<Item> filter)
		{
			Guard.ArgumentNotNull(filter, "filter");

			foreach (Item i in Items.Values)
			{
				if ((i.Sync.LastUpdate.When == null || i.Sync.LastUpdate.When >= since)
					&& filter(i))
					yield return i.Clone();
			}
		}

		public override void Delete(string id)
		{
			Items.Remove(id);
		}

		public override void Update(Item item)
		{
			Item i = item.Clone();
			Items[item.Sync.Id] = i;
		}

		public override IEnumerable<Item> Merge(IEnumerable<Item> items)
		{
			throw new NotSupportedException();
		}
	}
}
