using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;
using System.IO;
using System.Configuration;
using System.Xml;

namespace SimpleSharing.Tests
{

	public class MockXmlRepository : IXmlRepository
	{
		Dictionary<string, IXmlItem> items = new Dictionary<string, IXmlItem>();

		public Dictionary<string, IXmlItem> Items { get { return items; } }

		private XmlElement GetElement(string xml)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			return doc.DocumentElement;
		}

		public MockXmlRepository AddOneItem(string id)
		{
			items.Add(id, new XmlItem(id,
				"Foo Title", "Foo Description", DateTime.Now,
				GetElement("<Foo Title='Foo'/>")));

			return this;
		}

		public MockXmlRepository AddOneItem()
		{
			string id = Guid.NewGuid().ToString();
			items.Add(id, new XmlItem(id,
				"Foo Title", "Foo Description", DateTime.Now,
				GetElement("<Foo Title='Foo'/>")));

			return this;
		}

		public MockXmlRepository AddTwoItems()
		{
			string id = Guid.NewGuid().ToString();
			items.Add(id, new XmlItem(id,
				"Foo Title", "Foo Description", DateTime.Now,
				GetElement("<Foo Title='Foo'/>")));

			id = Guid.NewGuid().ToString();
			items.Add(id, new XmlItem(id,
				"Bar Title", "Bar Description", DateTime.Now,
				GetElement("<Foo Title='Bar'/>")));

			return this;
		}

		public MockXmlRepository AddThreeItemsByDays()
		{
			string id = Guid.NewGuid().ToString();
			items.Add(id, new XmlItem(id,
				"Foo Title", "Foo Description", DateTime.Now,
				GetElement("<Foo Title='Foo'/>")));

			id = Guid.NewGuid().ToString();
			items.Add(id, new XmlItem(id,
				"Bar Title", "Bar Description", DateTime.Now.Subtract(TimeSpan.FromDays(1)),
				GetElement("<Foo Title='Bar'/>")));

			id = Guid.NewGuid().ToString();
			items.Add(id, new XmlItem(id,
				"Baz Title", "Baz Description", DateTime.Now.Subtract(TimeSpan.FromDays(3)),
				GetElement("<Foo Title='Baz'/>")));

			return this;
		}

		public object Add(IXmlItem item)
		{
			Guard.ArgumentNotNull(item, "item");
			Guard.ArgumentNotNullOrEmptyString(item.Id, "item.Id");

			IXmlItem clone = item.Clone();
            clone.Hash = DateTime.Now;
            
			items.Add(item.Id, clone);

			return clone.Hash;
		}

		public bool Contains(string id)
		{
			Guard.ArgumentNotNullOrEmptyString(id, "id");

			return items.ContainsKey(id);
		}

		public IXmlItem Get(string id)
		{
			Guard.ArgumentNotNullOrEmptyString(id, "id");

			if (items.ContainsKey(id))
			{
				return items[id].Clone();
			}

			return null;
		}

		public bool Remove(string id)
		{
			return items.Remove(id);
		}

		public object Update(IXmlItem item)
		{
			Guard.ArgumentNotNull(item, "item");

			if (!items.ContainsKey(item.Id))
				throw new KeyNotFoundException();
            
			IXmlItem clone = item.Clone();
            clone.Hash = DateTime.Now;
            
			items[item.Id] = clone;

			return clone.Hash;
		}

		public IEnumerable<IXmlItem> GetAll()
		{
			foreach (IXmlItem item in items.Values)
			{
				yield return item.Clone();
			}
		}

		public IEnumerable<IXmlItem> GetAllSince(DateTime date)
		{
			foreach (IXmlItem item in items.Values)
			{
				if ((DateTime)item.Hash >= date)
					yield return item.Clone();
			}
		}

		public DateTime GetFirstUpdated()
		{
			if (items.Count == 0) return DateTime.MinValue;

			DateTime first = DateTime.MaxValue;

			foreach (IXmlItem item in items.Values)
			{
				if ((DateTime)item.Hash < first)
					first = (DateTime)item.Hash;
			}

			return first;
		}

		public DateTime GetFirstUpdated(DateTime since)
		{
			if (items.Count == 0) return since;

			DateTime first = DateTime.MaxValue;

			foreach (IXmlItem item in items.Values)
			{
				if ((DateTime)item.Hash < first && (DateTime)item.Hash > since)
					first = (DateTime)item.Hash;
			}

			return first;
		}

		public DateTime GetLastUpdated()
		{
			if (items.Count == 0) return DateTime.Now;

			DateTime last = DateTime.MinValue;

			foreach (IXmlItem item in items.Values)
			{
				if ((DateTime)item.Hash > last)
					last = (DateTime)item.Hash;
			}

			return last;
		}
	}
}
