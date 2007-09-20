using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSharing.Tests
{
	public class MockRepository : IRepository
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

		public string FriendlyName
		{
			get { return name; }
		}

		public bool SupportsMerge
		{
			get { return false; }
		}

		public Item Get(string id)
		{
			if (Items.ContainsKey(id))
				return Items[id].Clone();
			else
				return null;
		}

		public IEnumerable<Item> GetAll()
		{
			return Items.Values;
		}

		public IEnumerable<Item> GetAllSince(DateTime? since)
		{
			foreach (Item i in Items.Values)
			{
				if (i.Sync.LastUpdate.When >= since)
					yield return i.Clone();
			}
		}

		public IEnumerable<Item> GetConflicts()
		{
			foreach (Item item in Items.Values)
			{
				if (item.Sync.Conflicts.Count > 0)
					yield return item;
			}
		}

		public void Delete(string id)
		{
			Items.Remove(id);
		}

		public void Update(Item item)
		{
			Item i = item.Clone();
			Items[item.Sync.Id] = i;
		}

		public void Update(Item item, bool resolveConflicts)
		{
			if (resolveConflicts)
			{
				item = Behaviors.ResolveConflicts(item, DeviceAuthor.Current, DateTime.Now, item.Sync.Deleted);
			}
			Item i = item.Clone();
			Items[item.Sync.Id] = i;
		}

		public IList<Item> Merge(IEnumerable<Item> items)
		{
			throw new NotSupportedException();
		}

		public void Add(Item item)
		{
			Items.Add(item.Sync.Id, item);
		}
	}
}
