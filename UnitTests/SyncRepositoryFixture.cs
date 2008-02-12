#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using SimpleSharing;
using System.Xml;
using System.Threading;
using System.ServiceModel.Syndication;

namespace FeedSync.Tests
{
	/// <summary>
	/// Base class for fixtures of implementations of <see cref="ISyncRepository"/>.
	/// </summary>
	[TestClass]
	public class SyncRepositoryFixture : TestFixtureBase
	{
		protected virtual ISyncRepository CreateRepository()
		{
			return new MockSyncRepository();
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfSaveNullSync()
		{
			ISyncRepository repo = CreateRepository();

			repo.Save(null);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfGetNullId()
		{
			ISyncRepository repo = CreateRepository();

			repo.Get(null);
		}

		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void ShouldThrowIfGetEmptyId()
		{
			ISyncRepository repo = CreateRepository();

			repo.Get("");
		}

		[TestMethod]
		public void ShouldSaveSync()
		{
			ISyncRepository repo = CreateRepository();
			string id = Guid.NewGuid().ToString();

			repo.Save(Sync.Create(id, "kzu", DateTime.Now));

			Sync sync = repo.Get(id);

			Assert.IsNotNull(sync);
			Assert.AreEqual(id, sync.Id);
		}

		[TestMethod]
		public void ShouldGetAll()
		{
			ISyncRepository repo = CreateRepository();
			repo.Save(Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now));
			repo.Save(Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now));
			repo.Save(Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now));

			IEnumerable<Sync> syncs = repo.GetAll();

			Assert.AreEqual(3, Count(syncs));
		}

		[TestMethod]
		public void ShouldGetAllConflicts()
		{
			ISyncRepository repo = CreateRepository();
			repo.Save(Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now));
			repo.Save(Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now));
			repo.Save(Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now));

			Sync s = Sync.Create(Guid.NewGuid().ToString(), "kzu", DateTime.Now);
			Sync conflict = s.Clone();
			s = s.Update("vcc", null);
			conflict = conflict.Update("ary", null);

			s.Conflicts.Add(new FeedSyncSyndicationItem("title", "summary", 
				new TextSyndicationContent("test"), conflict));

			repo.Save(s);

			IEnumerable<Sync> conflicts = repo.GetConflicts();

			Assert.AreEqual(1, Count(conflicts));
		}

		[TestMethod]
		public void ShouldGetNullItemIfMissing()
		{
			ISyncRepository repo = CreateRepository();
			Sync s = repo.Get(Guid.NewGuid().ToString());

			Assert.IsNull(s);
		}		
	}
}