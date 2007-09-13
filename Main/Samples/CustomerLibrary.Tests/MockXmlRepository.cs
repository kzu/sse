using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;
using System.IO;
using SimpleSharing;
using System.Xml;

namespace CustomerLibrary.Tests
{

	public class MockXmlRepository : IXmlRepository
	{
		Dictionary<string, IXmlItem> items = new Dictionary<string, IXmlItem>();

		private XmlElement GetElement(string xml)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			return doc.DocumentElement;
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
				GetElement("<Foo Title='Foo'/>")));

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
				GetElement("<Foo Title='Foo'/>")));

			id = Guid.NewGuid().ToString();
			items.Add(id, new XmlItem(id,
				"Baz Title", "Baz Description", DateTime.Now.Subtract(TimeSpan.FromDays(3)),
				GetElement("<Foo Title='Foo'/>")));

			return this;
		}

		public object Add(IXmlItem item)
		{
			Guard.ArgumentNotNullOrEmptyString(item.Id, "item.Id");

			IXmlItem clone = item.Clone();
			clone.Hash = DateTime.Now;

			items.Add(item.Id, clone);

			return clone.Hash;
		}

		public bool Contains(string id)
		{
			return items.ContainsKey(id);
		}

		public IXmlItem Get(string id)
		{
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
	}
}
