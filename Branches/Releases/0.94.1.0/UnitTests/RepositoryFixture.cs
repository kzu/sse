#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSharing.Tests
{
	[TestClass]
	public class RepositoryFixture : TestFixtureBase
	{
		[TestMethod]
		public void ShouldGetAllCallGetAllSinceWithNullSince()
		{
			SimpleRepository repo = new SimpleRepository();

			repo.GetAll();

			Assert.AreEqual(null, repo.Since);
		}

		[TestMethod]
		public void ShouldGetAllWithFilterPassToImplementation()
		{
			SimpleRepository repo = new SimpleRepository();

			repo.GetAll(MyFilter);

			Assert.AreEqual(new Predicate<Item>(MyFilter), repo.Filter);
		}

		[TestMethod]
		public void ShouldResolveConflicts()
		{
			MockRepository repo = new MockRepository();

			string id = Guid.NewGuid().ToString();
			Sync sync = Behaviors.Create(id, "kzu", DateTime.Now.Subtract(TimeSpan.FromDays(1)), false);

			Item item = new Item(new NullXmlItem(id),
				Behaviors.Update(sync, "kzu", DateTime.Now.Subtract(TimeSpan.FromHours(2)), false));
			sync.Conflicts.Add(
				new Item(
					new XmlItem("foo", "bar", GetElement("<payload/>")),
					Behaviors.Update(sync, "kzu", DateTime.Now.Subtract(TimeSpan.FromHours(4)), false)));

			repo.Add(item);

			repo.Update(item, true);

			item = repo.Get(id);

			Assert.AreEqual(0, item.Sync.Conflicts.Count);
		}

		private bool MyFilter(Item item)
		{
			return true;
		}

		class SimpleRepository : Repository
		{
			public DateTime? Since;
			public Predicate<Item> Filter;

			public override bool SupportsMerge
			{
				get { throw new NotImplementedException("The method or operation is not implemented."); }
			}

			public override Item Get(string id)
			{
				throw new NotImplementedException("The method or operation is not implemented.");
			}

			public override IEnumerable<Item> GetAllSince(DateTime? since, Predicate<Item> filter)
			{
				Since = since;
				Filter = filter;

				return new Item[0];
			}

			public override void Add(Item item)
			{
				throw new NotImplementedException("The method or operation is not implemented.");
			}

			public override void Delete(string id)
			{
				throw new NotImplementedException("The method or operation is not implemented.");
			}

			public override void Update(Item item)
			{
				throw new NotImplementedException("The method or operation is not implemented.");
			}

			public override IEnumerable<Item> Merge(IEnumerable<Item> items)
			{
				throw new NotImplementedException("The method or operation is not implemented.");
			}

			public override string FriendlyName
			{
				get { throw new NotImplementedException("The method or operation is not implemented."); }
			}
		}
	}
}
