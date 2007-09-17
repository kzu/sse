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

namespace SimpleSharing.Tests
{
	/// <summary>
	/// Base class for fixtures of implementations of <see cref="IXmlRepository"/>.
	/// </summary>
	[TestClass]
	public class XmlRepositoryFixture : TestFixtureBase
	{
		protected virtual IXmlRepository CreateRepository()
		{
			return new MockXmlRepository();
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfAddNullItem()
		{
			CreateRepository().Add(null);
		}

		[TestMethod]
		public void ShouldAddReturnItemTimestamp()
		{
			IXmlItem item = new MockXmlItem();
			item.Timestamp = item.Timestamp.Subtract(TimeSpan.FromDays(1));
			DateTime original = item.Timestamp;
			DateTime timestamp = CreateRepository().Add(item);

			Assert.AreNotEqual(original, timestamp);
		}

		[TestMethod]
		public void ShouldContainAfterAdd()
		{
			IXmlItem item = new MockXmlItem();
			IXmlRepository repo = CreateRepository();

			repo.Add(item);

			Assert.IsTrue(repo.Contains(item.Id));
		}

		[TestMethod]
		public void ShouldGetAddedItem()
		{
			IXmlItem item = new MockXmlItem();
			IXmlRepository repo = CreateRepository();
			repo.Add(item);

			IXmlItem item2 = repo.Get(item.Id);

			Assert.IsNotNull(item2);
		}

		[TestMethod]
		public void ShouldGetNullIfNonExistingId()
		{
			IXmlItem item = CreateRepository().Get("1");

			Assert.IsNull(item);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfGetNullId()
		{
			CreateRepository().Get(null);
		}

		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void ShouldThrowIfGetEmptyId()
		{
			CreateRepository().Get(string.Empty);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfContainsNullId()
		{
			CreateRepository().Contains(null);
		}

		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void ShouldThrowIfContainsEmptyId()
		{
			CreateRepository().Contains(string.Empty);
		}

		[TestMethod]
		public void ShouldEnumerateAllItems()
		{
			IXmlRepository repo = CreateRepository();
			repo.Add(new MockXmlItem());
			repo.Add(new MockXmlItem());
			repo.Add(new MockXmlItem());

			IEnumerable<IXmlItem> items = repo.GetAll();

			Assert.AreEqual(3, Count(items));
		}

		[TestMethod]
		public void ShouldGetAllSinceDate()
		{
			IXmlRepository repo = CreateRepository();
			repo.Add(new MockXmlItem());
			Thread.Sleep(1000);

			DateTime now = Timestamp.Normalize(DateTime.Now);

			Thread.Sleep(1000);

			repo.Add(new MockXmlItem());
			repo.Add(new MockXmlItem());

			IEnumerable<IXmlItem> items = repo.GetAllSince(now);

			Assert.AreEqual(2, Count(items));
		}

		[TestMethod]
		public void ShouldRemoveFalseIfNonExitingId()
		{
			IXmlRepository repo = CreateRepository();

			bool removed = repo.Remove("1");

			Assert.IsFalse(removed);
		}

		[TestMethod]
		public void ShouldRemoveTrueForExistingId()
		{
			IXmlRepository repo = CreateRepository();
			IXmlItem item = new MockXmlItem();
			repo.Add(item);

			bool removed = repo.Remove(item.Id);

			Assert.IsTrue(removed);
		}

		[TestMethod]
		public void ShouldNotReturnSameItemInstanceButEqual()
		{
			IXmlRepository repo = CreateRepository();
			IXmlItem item = new MockXmlItem();
			repo.Add(item);

			IXmlItem item2 = repo.Get(item.Id);

			Assert.AreNotSame(item, item2);
		}

		[TestMethod]
		public void ShouldUpdateItem()
		{
			IXmlRepository repo = CreateRepository();
			IXmlItem item = new MockXmlItem();
			DateTime original = repo.Add(item);

			item.Payload.InnerXml = "<foo>updated</foo>";

			DateTime dt = repo.Update(item);

			Assert.AreNotEqual(original, dt);

			IXmlItem item2 = repo.Get(item.Id);

			Assert.AreEqual("<payload><foo>updated</foo></payload>", item2.Payload.OuterXml);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldUpdateThrowIfNullItem()
		{
			CreateRepository().Update(null);
		}
	}
}
