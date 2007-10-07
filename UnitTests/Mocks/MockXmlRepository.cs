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
        Dictionary<string, MockXmlItem> items = new Dictionary<string, MockXmlItem>();

        private XmlElement GetElement(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.DocumentElement;
        }

        public MockXmlRepository AddOneItem()
        {
            string id = Guid.NewGuid().ToString();
            items.Add(id, new MockXmlItem(new XmlItem(id,
                "Foo Title", "Foo Description",
                GetElement("<Foo Title='Foo'/>")), DateTime.Now));

            return this;
        }

        public MockXmlRepository AddTwoItems()
        {
            string id = Guid.NewGuid().ToString();
            items.Add(id, new MockXmlItem(new XmlItem(id,
                "Foo Title", "Foo Description",
                GetElement("<Foo Title='Foo'/>")), DateTime.Now));

            id = Guid.NewGuid().ToString();
            items.Add(id, new MockXmlItem(new XmlItem(id,
                "Bar Title", "Bar Description",
                GetElement("<Foo Title='Foo'/>")), DateTime.Now));

            return this;
        }

        public MockXmlRepository AddThreeItemsByDays()
        {
            string id = Guid.NewGuid().ToString();
            items.Add(id, new MockXmlItem(new XmlItem(id,
                "Foo Title", "Foo Description",
                GetElement("<Foo Title='Foo'/>")), DateTime.Now));

            id = Guid.NewGuid().ToString();
            items.Add(id, new MockXmlItem(new XmlItem(id,
                "Bar Title", "Bar Description",
                GetElement("<Foo Title='Foo'/>")), DateTime.Now.Subtract(TimeSpan.FromDays(1))));

            id = Guid.NewGuid().ToString();
            items.Add(id, new MockXmlItem(new XmlItem(id,
                "Baz Title", "Baz Description",
                GetElement("<Foo Title='Foo'/>")), DateTime.Now.Subtract(TimeSpan.FromDays(3))));

            return this;
        }

        public void Add(IXmlItem item)
        {
            Guard.ArgumentNotNull(item, "item");
            Guard.ArgumentNotNullOrEmptyString(item.Id, "item.Id");

            IXmlItem clone = item.Clone();

            items.Add(item.Id, new MockXmlItem(clone, DateTime.Now));
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
                return items[id].Item.Clone();
            }

            return null;
        }

        public bool Remove(string id)
        {
            return items.Remove(id);
        }

        public void Update(IXmlItem item)
        {
            Guard.ArgumentNotNull(item, "item");
            Guard.ArgumentNotNullOrEmptyString(item.Id, "item.Id");

            if (!items.ContainsKey(item.Id))
                throw new KeyNotFoundException();

            IXmlItem clone = item.Clone();
            items[item.Id].Item = clone;
            items[item.Id].Timestamp = DateTime.Now;
        }

        public IEnumerable<IXmlItem> GetAll()
        {
            foreach (MockXmlItem item in items.Values)
            {
                yield return item.Item.Clone();
            }
        }

        public IEnumerable<IXmlItem> GetAllSince(DateTime date)
        {
            foreach (MockXmlItem item in items.Values)
            {
                if (item.Timestamp >= date)
                    yield return item.Item.Clone();
            }
        }

		public DateTime GetFirstUpdated()
		{
			if (items.Count == 0) return DateTime.MinValue;

			DateTime first = DateTime.MaxValue;

			foreach (MockXmlItem item in items.Values)
			{
				if (item.Timestamp < first)
					first = item.Timestamp;
			}

			return first;
		}

		public DateTime GetFirstUpdated(DateTime since)
		{
			if (items.Count == 0) return since;

			DateTime first = DateTime.MaxValue;

			foreach (MockXmlItem item in items.Values)
			{
				if (item.Timestamp < first && item.Timestamp > since)
					first = item.Timestamp;
			}

			return first;
		}

		public DateTime GetLastUpdated()
		{
			if (items.Count == 0) return DateTime.Now;

			DateTime last = DateTime.MinValue;

			foreach (MockXmlItem item in items.Values)
			{
				if (item.Timestamp > last)
					last = item.Timestamp;
			}

			return last;
        }

        class MockXmlItem
        {
            public IXmlItem Item;
            public DateTime Timestamp;

            public MockXmlItem(IXmlItem item, DateTime timestamp)
            {
                Item = item;
                Timestamp = timestamp;
            }
        }

    }
}
