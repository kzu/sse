#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System;
using System.Collections.Generic;
using System.Threading;

namespace SimpleSharing.Tests
{
	[TestClass]
	public class CompoundRepositoryFixture : TestFixtureBase
	{
		IXmlRepository xmlRepo;
		ISyncRepository syncRepo;

		[TestInitialize]
		public void SetUp()
		{
			xmlRepo = new MockXmlRepository();
			syncRepo = new MockSyncRepository();
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfNullXmlRepo()
		{
			new CompoundRepository(null, syncRepo);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfNullSyncRepo()
		{
			new CompoundRepository(xmlRepo, null);
		}

		[TestMethod]
		public void ShouldNotSupportMerge()
		{
			CompoundRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			Assert.IsFalse(repo.SupportsMerge);
		}

		[TestMethod]
		public void ShouldExposeInnerRepositories()
		{
			CompoundRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			Assert.AreSame(xmlRepo, repo.XmlRepository);
			Assert.AreSame(syncRepo, repo.SyncRepository);
		}

		[TestMethod]
		public void ShouldGetExistingItem()
		{
			XmlItem item = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(item.Id, "kzu", DateTime.Now, false);
			sync.ItemHash = item.GetHashCode();
			xmlRepo.Add(item);
			syncRepo.Save(sync);

			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			Item i = repo.Get(item.Id);

			Assert.AreEqual(item.GetHashCode(), i.XmlItem.GetHashCode());
			Assert.AreEqual(sync.GetHashCode(), i.Sync.GetHashCode());
		}

		[TestMethod]
		public void ShouldGetUpdatedItemIfHashChanged()
		{
			XmlItem item = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(item.Id, "kzu", DateTime.Now, false);
			sync.ItemHash = item.GetHashCode();
			xmlRepo.Add(item);
			syncRepo.Save(sync);

			item.Title = "changed";
			xmlRepo.Update(item);

			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			Item i = repo.Get(item.Id);

			Assert.AreEqual(item.GetHashCode(), i.XmlItem.GetHashCode());
			Assert.AreEqual(2, i.Sync.Updates);
		}

		[TestMethod]
		public void ShouldGetCreateNewSyncForNewXmlItem()
		{
			XmlItem item = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			xmlRepo.Add(item);

			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			Item i = repo.Get(item.Id);

			Assert.AreEqual(item.GetHashCode(), i.XmlItem.GetHashCode());
			Assert.AreEqual(1, i.Sync.Updates);
		}

		[TestMethod]
		public void ShouldGetDeletedItem()
		{
			XmlItem item = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(item.Id, "kzu", DateTime.Now, false);
			sync.ItemHash = item.GetHashCode();
			//Not added simulates deleted.
			//xmlRepo.Add(item);
			syncRepo.Save(sync);

			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			Item i = repo.Get(item.Id);

			Assert.IsTrue(i.Sync.Deleted);
			Assert.AreEqual(2, i.Sync.Updates);
		}

		[TestMethod]
		public void ShouldGetNullIfNotExists()
		{
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			Item i = repo.Get(Guid.NewGuid().ToString());

			Assert.IsNull(i);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowGetNullId()
		{
			new CompoundRepository(xmlRepo, syncRepo).Get(null);
		}

		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void ShouldThrowGetEmptyId()
		{
			new CompoundRepository(xmlRepo, syncRepo).Get("");
		}

		[TestMethod]
		public void ShouldGetAll()
		{
			XmlItem item = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(item.Id, "kzu", DateTime.Now, false);
			sync.ItemHash = item.GetHashCode();
			xmlRepo.Add(item);
			syncRepo.Save(sync);

			item = new XmlItem(Guid.NewGuid().ToString(), "bar", "baz", GetElement("<payload />"));
			sync = Behaviors.Create(item.Id, "kzu", DateTime.Now, false);
			sync.ItemHash = item.GetHashCode();
			xmlRepo.Add(item);
			syncRepo.Save(sync);

			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			List<Item> items = new List<Item>(repo.GetAll());

			Assert.AreEqual(2, items.Count);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowGetAllWithNullFilter()
		{
			new CompoundRepository(xmlRepo, syncRepo).GetAll(null);
		}

		[TestMethod]
		public void ShouldGetAllWithAutoUpdatesToSync()
		{
			XmlItem item = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			xmlRepo.Add(item);

			item = new XmlItem(Guid.NewGuid().ToString(), "bar", "baz", GetElement("<payload />"));
			Sync sync = Behaviors.Create(item.Id, "kzu", DateTime.Now, false);
			sync.ItemHash = item.GetHashCode();
			xmlRepo.Add(item);
			syncRepo.Save(sync);

			item.Title = "changed";
			xmlRepo.Update(item);

			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			List<Item> items = new List<Item>(repo.GetAll());

			Assert.AreEqual(2, items.Count);
			Assert.AreEqual(1, items[0].Sync.Updates);
			Assert.AreEqual(2, items[1].Sync.Updates);
		}

		[TestMethod]
		public void ShouldGetAllIncludeDeleted()
		{
			// simulates a deleted item, there's sync but no item.
			Sync sync = Behaviors.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now, false);
			syncRepo.Save(sync);

			XmlItem item = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			sync = Behaviors.Create(item.Id, "kzu", DateTime.Now, false);
			sync.ItemHash = item.GetHashCode();
			xmlRepo.Add(item);
			syncRepo.Save(sync);

			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			List<Item> items = new List<Item>(repo.GetAll());

			Assert.AreEqual(2, items.Count);
			Assert.IsTrue(items[1].Sync.Deleted);
		}

		[TestMethod]
		public void ShouldGetAllIncludeMultipleDeleted()
		{
			XmlItem item = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(item.Id, "kzu", DateTime.Now, false);
			sync.ItemHash = item.GetHashCode();
			xmlRepo.Add(item);
			syncRepo.Save(sync);

			// simulates a deleted item, there's sync but no item.
			sync = Behaviors.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now, false);
			syncRepo.Save(sync);
			sync = Behaviors.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now, false);
			syncRepo.Save(sync);
			sync = Behaviors.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now, false);
			syncRepo.Save(sync);

			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			List<Item> items = new List<Item>(repo.GetAll());

			Assert.AreEqual(4, items.Count);
		}

		[TestMethod]
		public void ShouldGetAllWithNullSyncWhen()
		{
			XmlItem item = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(item.Id, "kzu", null, false);
			sync.ItemHash = item.GetHashCode();
			xmlRepo.Add(item);
			syncRepo.Save(sync);

			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			Assert.AreEqual(1, Count(repo.GetAll()));
		}

		[TestMethod]
		public void ShouldGetAlwaysItemWithNullWhenLastUpdate()
		{
			XmlItem xml = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(xml.Id, "kzu", null, false);

			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			repo.Add(new Item(xml, sync));

			sync = Behaviors.Update(sync, "kzu", null, false);

			repo.Update(new Item(xml, sync));

			Assert.AreEqual(1, Count(repo.GetAllSince(DateTime.Now)));
		}

		[TestMethod]
		public void ShouldGetAllWithNullSince()
		{
			XmlItem item = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(item.Id, "kzu", null, false);
			sync.ItemHash = item.GetHashCode();
			xmlRepo.Add(item);
			syncRepo.Save(sync);

			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			Assert.AreEqual(1, Count(repo.GetAllSince(null)));
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowGetAllSinceWithNullFilter()
		{
			Count(new CompoundRepository(xmlRepo, syncRepo).GetAllSince(DateTime.Now, null));
		}

		[TestMethod]
		public void ShouldGetAllSinceRemoveMilliseconds()
		{
			DateTime since = new DateTime(2007, 9, 18, 12, 56, 23, 500);

			XmlItem item = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(item.Id, "kzu", since, false);
			sync.ItemHash = item.GetHashCode();
			xmlRepo.Add(item);
			syncRepo.Save(sync);

			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			Assert.AreEqual(1, Count(repo.GetAllSince(since)));
		}
		
		[TestMethod]
		public void ShouldGetAddedAfterSince()
		{
			XmlItem item = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(item.Id, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(5)), false);
			sync.ItemHash = item.GetHashCode();
			xmlRepo.Add(item);
			syncRepo.Save(sync);

			DateTime since = DateTime.Now;

			item = new XmlItem(Guid.NewGuid().ToString(), "bar", "baz", GetElement("<payload />"));
			sync = Behaviors.Create(item.Id, "kzu", DateTime.Now, false);
			sync.ItemHash = item.GetHashCode();
			xmlRepo.Add(item);
			syncRepo.Save(sync);

			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			List<Item> items = new List<Item>(repo.GetAllSince(since));

			Assert.AreEqual(1, items.Count);
		}

		[TestMethod]
		public void ShouldGetUpdatedAfterSince()
		{
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			XmlItem item = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(item.Id, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(5)), false);
			sync.ItemHash = item.GetHashCode();
			xmlRepo.Add(item);
			syncRepo.Save(sync);

			DateTime since = DateTime.Now;

			item = new XmlItem(Guid.NewGuid().ToString(), "bar", "baz", GetElement("<payload />"));
			sync = Behaviors.Create(item.Id, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(4)), false);
			sync.ItemHash = item.GetHashCode();
			xmlRepo.Add(item);
			syncRepo.Save(sync);

			Assert.AreEqual(0, Count(repo.GetAllSince(since)));

			sync = Behaviors.Update(sync, "kzu", DateTime.Now, false);
			syncRepo.Save(sync);
			
			List<Item> items = new List<Item>(repo.GetAllSince(since));

			Assert.AreEqual(1, items.Count);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfNullItem()
		{
			new CompoundRepository(xmlRepo, syncRepo).Add(null);
		}

		[TestMethod]
		public void ShouldAddItem()
		{
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			XmlItem xml = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(xml.Id, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(5)), false);
			sync.ItemHash = xml.GetHashCode();

			Item item = new Item(xml, sync);

			repo.Add(item);

			Assert.AreEqual(xml.GetHashCode(), xmlRepo.Get(xml.Id).GetHashCode());
			Assert.AreEqual(sync.GetHashCode(), syncRepo.Get(sync.Id).GetHashCode());
		}

		[TestMethod]
		public void ShouldAddItemSaveItemHash()
		{
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			XmlItem xml = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(xml.Id, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(5)), false);

			Item item = new Item(xml, sync);

			repo.Add(item);

			Item saved = repo.Get(xml.Id);

			Assert.AreEqual(saved.XmlItem.GetHashCode(), saved.Sync.ItemHash);
		}

		[TestMethod]
		public void ShouldAddOnlySyncForDeletedItem()
		{
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			XmlItem xml = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(xml.Id, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(5)), false);
			sync.ItemHash = xml.GetHashCode();
			sync = Behaviors.Delete(sync, "kzu", DateTime.Now);

			Item item = new Item(xml, sync);

			repo.Add(item);

			Assert.IsNull(xmlRepo.Get(xml.Id));
			Assert.AreEqual(sync.GetHashCode(), syncRepo.Get(sync.Id).GetHashCode());
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowDeleteNullId()
		{
			new CompoundRepository(xmlRepo, syncRepo).Delete(null);
		}

		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void ShouldThrowDeleteEmptyId()
		{
			new CompoundRepository(xmlRepo, syncRepo).Delete("");
		}

		[TestMethod]
		public void ShouldDeleteItem()
		{
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			XmlItem xml = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(xml.Id, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(5)), false);
			sync.ItemHash = xml.GetHashCode();
			xmlRepo.Add(xml);
			syncRepo.Save(sync);

			repo.Delete(xml.Id);

			Assert.IsNull(xmlRepo.Get(xml.Id));
			Assert.IsTrue(syncRepo.Get(sync.Id).Deleted);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowUpdateNullItem()
		{
			new CompoundRepository(xmlRepo, syncRepo).Update(null);
		}

		[TestMethod]
		public void ShouldUpdateItem()
		{
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			XmlItem xml = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(xml.Id, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(5)), false);
			sync.ItemHash = xml.GetHashCode();
			sync = Behaviors.Delete(sync, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(4)));
			xmlRepo.Add(xml);
			syncRepo.Save(sync);

			sync = Behaviors.Update(sync, "kzu", DateTime.Now, false);
			xml.Title = "changed";

			Item item = new Item(xml, sync);

			repo.Update(item);

			Assert.AreEqual(xml.GetHashCode(), xmlRepo.Get(xml.Id).GetHashCode()); 
			Assert.AreEqual(sync.GetHashCode(), syncRepo.Get(sync.Id).GetHashCode());
		}

		[TestMethod]
		public void ShouldUpdateAndGetItem()
		{
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			XmlItem xml = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(xml.Id, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(5)), false);
			sync.ItemHash = xml.GetHashCode();
			sync = Behaviors.Delete(sync, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(4)));
			xmlRepo.Add(xml);
			syncRepo.Save(sync);

			sync = Behaviors.Update(sync, "kzu", DateTime.Now, false);
			xml.Title = "changed";

			Item item = new Item(xml, sync);

			repo.Update(item);

			Item item2 = repo.Get(xml.Id);

			Assert.AreEqual(xml.GetHashCode(), item2.XmlItem.GetHashCode());
			Assert.AreEqual(sync.GetHashCode(), item2.Sync.GetHashCode());
		}

		[TestMethod]
		public void ShouldUpdateSaveItemHash()
		{
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			XmlItem xml = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(xml.Id, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(5)), false);
			sync.ItemHash = xml.GetHashCode();
			sync = Behaviors.Delete(sync, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(4)));
			xmlRepo.Add(xml);
			syncRepo.Save(sync);

			sync = Behaviors.Update(sync, "kzu", DateTime.Now, false);
			xml.Title = "changed";

			Item item = new Item(xml, sync);

			repo.Update(item);

			Item item2 = repo.Get(xml.Id);

			Assert.AreEqual(item2.XmlItem.GetHashCode(), item2.Sync.ItemHash);
		}

		[TestMethod]
		public void ShouldGetConflicts()
		{
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			XmlItem xml = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			Sync sync = Behaviors.Create(xml.Id, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(5)), false);
			sync.ItemHash = xml.GetHashCode();

			Item conflict = new Item(xml.Clone(), sync.Update("vga", DateTime.Now));
			conflict.XmlItem.Title = "bar";

			sync = sync.Update("kzu", DateTime.Now);
			sync.Conflicts.Add(conflict);
				
			xmlRepo.Add(xml);
			syncRepo.Save(sync);


			xml = new XmlItem(Guid.NewGuid().ToString(), "foo", "bar", GetElement("<payload />"));
			sync = Behaviors.Create(xml.Id, "kzu", DateTime.Now.Subtract(TimeSpan.FromMinutes(2)), false);
			sync.ItemHash = xml.GetHashCode();
			xmlRepo.Add(xml);
			syncRepo.Save(sync);

			List<Item> conflicts = new List<Item>(repo.GetConflicts());

			Assert.AreEqual(1, conflicts.Count);
		}

		[TestMethod]
		public void ShouldUpdateSyncLocalItemsOnUpdate()
		{
			ISyncRepository syncRepo = new MockSyncRepository();
			IXmlRepository xmlRepo = new MockXmlRepository();
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			string id = Guid.NewGuid().ToString();
			Sync sync = Behaviors.Create(id, DeviceAuthor.Current, DateTime.Now.Subtract(TimeSpan.FromMinutes(1)), false);
			Item item = new Item(
				new XmlItem(id, "foo", "bar",
					GetElement("<foo id='bar'/>")),
				sync);

			// Save original item.
			repo.Add(item);
			DateTime? lastUpdated = item.Sync.LastUpdate.When;

			// Local editing outside of SSE by the local app.
			xmlRepo.Update(new XmlItem(id, "changed", item.XmlItem.Description,
				item.XmlItem.Payload));

			// Export of same item should cause it to 
			// be updated with new Sync info and a no-op on sync.
			Item saved = GetFirst<Item>(repo.GetAll());

			Sync updated = saved.Sync;
			Assert.AreEqual(2, updated.Updates);
			Assert.AreNotEqual(lastUpdated, updated.LastUpdate.When);
		}

		[TestMethod]
		public void ShouldUpdateSyncDeletedOnGetConflictsIfItemIsDeleted()
		{
			ISyncRepository syncRepo = new MockSyncRepository();
			IXmlRepository xmlRepo = new MockXmlRepository().AddOneItem();
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			// Cause the item to be Sync'ed.
			Item item = GetFirst<Item>(repo.GetAll());

			// Introduce a conflict.
			Item clone = item.Clone();
			clone.XmlItem.Title = "Conflict";
			Sync updatedSync = Behaviors.Update(clone.Sync, "Conflict", DateTime.Now, false);
			item.Sync.Conflicts.Add(new Item(clone.XmlItem, updatedSync));
			syncRepo.Save(item.Sync);

			// Delete xml item.
			xmlRepo.Remove(item.XmlItem.Id);

			Item conflict = GetFirst<Item>(repo.GetConflicts());
			Assert.IsNotNull(conflict);
		}

		[TestMethod]
		public void ShouldSaveUpdatedItemOnResolveConflicts()
		{
			ISyncRepository syncRepo = new MockSyncRepository();
			IXmlRepository xmlRepo = new MockXmlRepository().AddOneItem();
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			// Cause the item to be Sync'ed.
			Item item = GetFirst<Item>(repo.GetAll());

			// Introduce a conflict.
			IXmlItem xml = item.XmlItem.Clone();
			xml.Title = "Conflict";
			Sync updatedSync = Behaviors.Update(item.Sync, "Conflict", DateTime.Now, false);
			item.Sync.Conflicts.Add(new Item(xml, updatedSync));

			item.XmlItem.Title = "Resolved";

			repo.Update(item, true);

			IXmlItem storedXml = xmlRepo.Get(item.XmlItem.Id);

			Assert.AreEqual("Resolved", storedXml.Title);
		}

		[TestMethod]
		public void ShouldSaveUpdatedItemOnResolveConflicts2()
		{
			ISyncRepository syncRepo = new MockSyncRepository();
			IXmlRepository xmlRepo = new MockXmlRepository().AddOneItem();
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			// Cause the item to be Sync'ed.
			Item item = GetFirst<Item>(repo.GetAll());

			// Introduce a conflict.
			IXmlItem xml = item.XmlItem.Clone();
			xml.Title = "Conflict";
			Sync updatedSync = Behaviors.Update(item.Sync, "Conflict", DateTime.Now, false);
			item.Sync.Conflicts.Add(new Item(xml, updatedSync));

			item.XmlItem.Title = "Resolved";

			repo.Update(item, true);

			IXmlItem storedXml = xmlRepo.Get(item.XmlItem.Id);

			Assert.AreEqual("Resolved", storedXml.Title);
		}

		[TestMethod]
		public void ShouldResolveConflictsPreserveDeletedState()
		{
			ISyncRepository syncRepo = new MockSyncRepository();
			IXmlRepository xmlRepo = new MockXmlRepository().AddOneItem();
			IRepository repo = new CompoundRepository(xmlRepo, syncRepo);

			// Cause the item to be Sync'ed.
			Item item = GetFirst<Item>(repo.GetAll());

			// Introduce a conflict.
			IXmlItem xml = item.XmlItem.Clone();
			xml.Title = "Conflict";
			Sync updatedSync = Behaviors.Update(item.Sync, "Conflict", DateTime.Now, false);
			item.Sync.Conflicts.Add(new Item(xml, updatedSync));

			item.XmlItem.Title = "Resolved";

			Sync deletedSync = Behaviors.Delete(item.Sync, "Deleted", DateTime.Now);

			repo.Update(new Item(item.XmlItem, deletedSync), true);

			IXmlItem storedXml = xmlRepo.Get(item.XmlItem.Id);

			Assert.IsNull(storedXml);
			Assert.IsTrue(syncRepo.Get(item.Sync.Id).Deleted);
		}

	}
}
