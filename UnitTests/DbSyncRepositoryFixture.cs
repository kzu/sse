#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
using Microsoft.Practices.Mobile.DataAccess;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Practices.EnterpriseLibrary.Data.SqlCe;
using Microsoft.Practices.EnterpriseLibrary.Data;
#endif

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Data.SqlServerCe;
using System.IO;
using System.Data.Common;

using System.Data;

namespace SimpleSharing.Tests
{
	[TestClass]
	public class DbSyncRepositoryFixture : TestFixtureBase
	{
		protected Database database;

		[TestInitialize]
		public virtual void Initialize()
		{
			if (File.Exists("SyncDb.sdf"))
				File.Delete("SyncDb.sdf");

			new SqlCeEngine("Data Source=SyncDb.sdf").CreateDatabase();
#if PocketPC
			this.database = new SqlDatabase("Data Source=SyncDb.sdf");
#else
			this.database = new SqlCeDatabase("Data Source=SyncDb.sdf");
#endif
		}

		[TestCleanup]
		public virtual void Cleanup()
		{
#if PocketPC
			database.GetConnection().Close();
#else
			((SqlCeDatabase)database).CloseSharedConnection();
#endif
		}

		protected virtual ISyncRepository CreateRepository(Database database, string repositoryId)
		{
			return new DbSyncRepository(database, repositoryId);
		}

		[TestMethod]
		public void ShouldAllowNullRepositoryId()
		{
			CreateRepository(database, null);
		}

		[TestMethod]
		public void ShouldAllowEmptyRepositoryId()
		{
			CreateRepository(database, "");
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfNullDatabase()
		{
			CreateRepository(null, "Foo");
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfNullItemTimestamp()
		{
			ISyncRepository repo = CreateRepository(database, "Foo");
			Sync s = new Sync(Guid.NewGuid().ToString());

			repo.Save(s);
		}

		[TestMethod]
		public void ShouldAddSync()
		{
			ISyncRepository repo = CreateRepository(database, "Foo");
			Sync s = new Sync(Guid.NewGuid().ToString(), 50);
			s.ItemTimestamp = DateTime.Now;

			repo.Save(s);

			Sync s2 = repo.Get(s.Id);
			Assert.AreEqual(s.Updates, s2.Updates);
		}

		[TestMethod]
		public void ShouldGetAllSync()
		{
			ISyncRepository repo = CreateRepository(database, "Foo");
			Sync s = new Sync(Guid.NewGuid().ToString(), 50);
			s.ItemTimestamp = DateTime.Now;

			repo.Save(s);
			s = new Sync(Guid.NewGuid().ToString());
			s.ItemTimestamp = DateTime.Now;
			repo.Save(s);

			Assert.AreEqual(2, Count(repo.GetAll()));
		}

		[TestMethod]
		public void ShouldGetConflictSync()
		{
			ISyncRepository repo = CreateRepository(database, "Foo");
			Sync s = new Sync(Guid.NewGuid().ToString(), 50);
			s.ItemTimestamp = DateTime.Now;

			repo.Save(s);
			s = new Sync(Guid.NewGuid().ToString());
			s.Conflicts.Add(new Item(new XmlItem("title", "desc", GetElement("<payload/>")),
				Behaviors.Update(s.Clone(), "foo", DateTime.Now, false)));
			s.ItemTimestamp = DateTime.Now;
			repo.Save(s);

			Assert.AreEqual(1, Count(repo.GetConflicts()));
		}

		[TestMethod]
		public void ShouldGetNullItemIfMissing()
		{
			ISyncRepository repo = CreateRepository(database, "Foo");
			Sync s = repo.Get(Guid.NewGuid().ToString());

			Assert.IsNull(s);
		}

		[TestMethod]
		public void ShouldGetNullLastSync()
		{
			ISyncRepository repo = CreateRepository(database, "Foo");

			Assert.IsNull(repo.GetLastSync("foo"));
		}

		[TestMethod]
		public void ShouldSaveLastSync()
		{
			DateTime now = Timestamp.Normalize(DateTime.Now);
			ISyncRepository repo = CreateRepository(database, "Foo");

			repo.SetLastSync("foo", now);

			Assert.AreEqual(now, repo.GetLastSync("foo"));
		}
	}
}
