#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System;

namespace SimpleSharing.Tests
{
	[TestClass]
	public class FeedFixture
	{
		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfTitleNull()
		{
			new Feed(null, "link", "description");
		}

		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void ShouldThrowIfTitleEmpty()
		{
			new Feed(String.Empty, "link", "description");
		}

		[TestMethod]
		public void ShouldNotThrowIfDescriptionNull()
		{
			new Feed("title", "link", null);
		}

		[TestMethod]
		public void ShouldNotThrowIfDescriptionEmpty()
		{
			new Feed("title", "link", String.Empty);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfLinkNull()
		{
			new Feed("title", null, "description");
		}

		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void ShouldThrowIfLinkEmpty()
		{
			new Feed("title", "", "description");
		}

		[TestMethod]
		public void ShouldMatchConstructorWithProperties()
		{
			Feed f = new Feed("title", "link", "description");
			Assert.AreEqual("title", f.Title);
			Assert.AreEqual("link", f.Link);
			Assert.AreEqual("description", f.Description);
		}
	}
}
