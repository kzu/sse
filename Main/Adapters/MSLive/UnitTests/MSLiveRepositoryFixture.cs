using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleSharing;
using SimpleSharing.Tests;
using System.Net;

namespace SimpleSharing.Adapters.MSLive.Tests
{
	[TestClass]
	public class MSLiveRepositoryFixture : TestFixtureBase
	{
		string feedUrl;

		[TestCleanup]
		public void Cleanup()
		{
			if (!string.IsNullOrEmpty(feedUrl))
				MSLiveHelper.Delete(feedUrl);
		}

		[TestMethod]
		public void ShouldSupportMerge()
		{
			Assert.IsTrue(new MSLiveRepository("http://sse.mslivelabs.com").SupportsMerge);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfNullFeedUrl()
		{
			new MSLiveRepository(null);
		}

		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void ShouldThrowIfEmptyString()
		{
			new MSLiveRepository("");
		}

		[ExpectedException(typeof(UriFormatException))]
		[TestMethod]
		public void ShouldThrowIfInvalidUri()
		{
			new MSLiveRepository("foo");
		}

		[TestMethod]
		public void ShouldOverrideDefaultTimeout()
		{
			MSLiveRepository repo = new MSLiveRepository("http://foo");
			repo.TimeoutSeconds = 1;

			DateTime now = DateTime.Now;

			try
			{
				List<Item> items = new List<Item>(repo.GetAll());
				Assert.Fail();
			}
			catch (WebException)
			{
				Assert.IsTrue(DateTime.Now - now < TimeSpan.FromSeconds(60), "Didn't override the 60 sec default timeout");
			}
		}

		[TestMethod]
		public void ShouldGetEmptyItems()
		{
			feedUrl = MSLiveHelper.Create("Foo", "Bar", "http://foo");

			IRepository repo = new MSLiveRepository(feedUrl);

			Assert.AreEqual(0, Count(repo.GetAll()));
		}

		[TestMethod]
		public void ShouldAddItem()
		{
			feedUrl = MSLiveHelper.Create("Foo", "Bar", "http://foo");
			IRepository repo = new MSLiveRepository(feedUrl);

			string id = Guid.NewGuid().ToString();
			Item item = new Item(
				new XmlItem("Hello", "World", GetElement("<payload><kzu>hi</kzu></payload>")),
				Behaviors.Create(id, DeviceAuthor.Current, DateTime.Now, false));

			repo.Add(item);

			Item saved = GetFirst<Item>(repo.GetAll());

			Assert.IsNotNull(saved);
			Assert.AreEqual(item.XmlItem.Title, saved.XmlItem.Title);
			Assert.AreEqual(item.XmlItem.Description, saved.XmlItem.Description);
			Assert.AreEqual(item.Sync.LastUpdate.By, saved.Sync.LastUpdate.By);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfGetNullId()
		{
			new MSLiveRepository("http://foo").Get(null);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfGetEmptyId()
		{
			new MSLiveRepository("http://foo").Get("");
		}

		[TestMethod]
		public void ShouldGetItemById()
		{
			feedUrl = MSLiveHelper.Create("Foo", "Bar", "http://foo");
			IRepository repo = new MSLiveRepository(feedUrl);

			string id = Guid.NewGuid().ToString();
			Item item = new Item(
				new XmlItem("Hello", "World", GetElement("<payload><kzu>hi</kzu></payload>")),
				Behaviors.Create(id, DeviceAuthor.Current, DateTime.Now, false));

			repo.Add(item);

			Item saved = repo.Get(id);

			Assert.IsNotNull(saved);
			Assert.AreEqual(item.XmlItem.Title, saved.XmlItem.Title);
			Assert.AreEqual(item.XmlItem.Description, saved.XmlItem.Description);
			Assert.AreEqual(item.Sync.LastUpdate.By, saved.Sync.LastUpdate.By);
		}

		[TestMethod]
		public void ShouldGetNullItemIfNotExists()
		{
			feedUrl = MSLiveHelper.Create("Foo", "Bar", "http://foo");
			IRepository repo = new MSLiveRepository(feedUrl);

			string id = Guid.NewGuid().ToString();
			Item saved = repo.Get(id);

			Assert.IsNull(saved);
		}

		[TestMethod]
		public void ShouldGetAllSince()
		{
			feedUrl = MSLiveHelper.Create("Foo", "Bar", "http://foo");
			IRepository repo = new MSLiveRepository(feedUrl);

			string id = Guid.NewGuid().ToString();
			Item item = new Item(
				new XmlItem("Hello", "World", GetElement("<payload><kzu>hi</kzu></payload>")),
				Behaviors.Create(id, DeviceAuthor.Current, DateTime.Now.Subtract(TimeSpan.FromMinutes(5)), false));

			repo.Add(item);

			id = Guid.NewGuid().ToString();
			item = new Item(
				new XmlItem("Hello", "World", GetElement("<payload><kzu>hi</kzu></payload>")),
				Behaviors.Create(id, DeviceAuthor.Current, DateTime.Now, false));

			Assert.AreEqual(1, Count(repo.GetAllSince(DateTime.Now.Subtract(TimeSpan.FromMinutes(1)))));
		}

		[ExpectedException(typeof(NotSupportedException))]
		[TestMethod]
		public void ShouldThrowNotSupportedOnDelete()
		{
			new MSLiveRepository("http://foo").Delete(Guid.NewGuid().ToString());
		}

		[TestMethod]
		public void ShouldUpdateItem()
		{
			feedUrl = MSLiveHelper.Create("Foo", "Bar", "http://foo");
			IRepository repo = new MSLiveRepository(feedUrl);

			string id = Guid.NewGuid().ToString();
			Item item = new Item(
				new XmlItem("Hello", "World", GetElement("<payload><kzu>hi</kzu></payload>")),
				Behaviors.Create(id, DeviceAuthor.Current, DateTime.Now.Subtract(TimeSpan.FromDays(2)), false));

			repo.Add(item);

			item = new Item(item.XmlItem,
				Behaviors.Update(item.Sync, DeviceAuthor.Current, DateTime.Now, false));
			item.XmlItem.Title = "Bye";

			repo.Update(item);

			Item saved = repo.Get(id);

			Assert.AreEqual("Bye", saved.XmlItem.Title);
			Assert.AreEqual(DateTime.Today, saved.Sync.LastUpdate.When.Value.Date);
		}

		[TestMethod]
		public void ShouldMergeItem()
		{
			feedUrl = MSLiveHelper.Create("Foo", "Bar", "http://foo");
			MSLiveRepository repo = new MSLiveRepository(feedUrl);

			string id = Guid.NewGuid().ToString();
			Item item1 = new Item(
				new XmlItem("Hello", "World", GetElement("<payload/>")),
				Behaviors.Create(id, DeviceAuthor.Current, DateTime.Now.Subtract(TimeSpan.FromHours(1)), false));

			repo.Add(item1);

			item1 = new Item(item1.XmlItem,
				Behaviors.Update(item1.Sync, DeviceAuthor.Current, DateTime.Now, false));
			item1.XmlItem.Title = "Changed";

			id = Guid.NewGuid().ToString();
			Item item2 = new Item(
				new XmlItem("NoChanges", "World", GetElement("<payload/>")),
				Behaviors.Create(id, DeviceAuthor.Current, DateTime.Now, false));

			repo.Add(item2);

			id = Guid.NewGuid().ToString();
			Item item3 = new Item(
				new XmlItem("New", "World", GetElement("<payload/>")),
				Behaviors.Create(id, DeviceAuthor.Current, DateTime.Now, false));

			repo.Merge(new Item[] { item1, item2, item3 });

			Dictionary<string, Item> items = new Dictionary<string, Item>();
			new List<Item>(repo.GetAll()).ForEach(delegate(Item current)
			{
				items.Add(current.Sync.Id, current);
			});

			Assert.AreEqual(3, items.Count);
			Assert.AreEqual("Changed", items[item1.Sync.Id].XmlItem.Title);
			Assert.AreEqual("NoChanges", items[item2.Sync.Id].XmlItem.Title);
			Assert.AreEqual("New", items[item3.Sync.Id].XmlItem.Title);
		}

		[ExpectedException(typeof(InvalidOperationException))]
		[TestMethod]
		public void ShouldThrowNotInitializedAdd()
		{
			MSLiveRepository repo = new MSLiveRepository();
			repo.Add(new Item(new MockXmlItem(), new Sync(Guid.NewGuid().ToString())));
		}

		[ExpectedException(typeof(InvalidOperationException))]
		[TestMethod]
		public void ShouldThrowNotInitializedGet()
		{
			MSLiveRepository repo = new MSLiveRepository();
			repo.Get(Guid.NewGuid().ToString());
		}

		[ExpectedException(typeof(InvalidOperationException))]
		[TestMethod]
		public void ShouldThrowNotInitializedGetAll()
		{
			MSLiveRepository repo = new MSLiveRepository();
			repo.GetAll();
		}

		[ExpectedException(typeof(InvalidOperationException))]
		[TestMethod]
		public void ShouldThrowNotInitializedGetAllSince()
		{
			MSLiveRepository repo = new MSLiveRepository();
			repo.GetAllSince(DateTime.Now);
		}

		[ExpectedException(typeof(InvalidOperationException))]
		[TestMethod]
		public void ShouldThrowNotInitializedGetConflicts()
		{
			MSLiveRepository repo = new MSLiveRepository();
			repo.GetConflicts();
		}

		[ExpectedException(typeof(InvalidOperationException))]
		[TestMethod]
		public void ShouldThrowNotInitializedMerge()
		{
			MSLiveRepository repo = new MSLiveRepository();
			repo.Merge(new Item[0]);
		}

		[ExpectedException(typeof(InvalidOperationException))]
		[TestMethod]
		public void ShouldThrowNotInitializedUpdate()
		{
			MSLiveRepository repo = new MSLiveRepository();
			repo.Update(new Item(new MockXmlItem(), new Sync("foo")));
		}
	}
}
