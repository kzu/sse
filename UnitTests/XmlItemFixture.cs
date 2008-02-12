using System;
#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ServiceModel.Syndication;
#endif

namespace FeedSync.Tests
{
	[TestClass]
	public class XmlItemFixture : TestFixtureBase
	{
		[TestMethod]
		public void ShouldGetHashcodeWithNullDescription()
		{
			XmlItem i1 = new XmlItem(Guid.NewGuid().ToString(), "title", null, new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml));
			XmlItem i2 = new XmlItem(i1.Id, "title", null, new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml));

			Assert.AreEqual(i1, i2);
			Assert.AreEqual(i1.GetHashCode(), i2.GetHashCode());
		}

		[TestMethod]
		public void ShoudAllowNullTitle()
		{
			XmlItem item = new XmlItem("id", null, "description", new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml));

			Assert.IsNull(item.Title);
		}

		[TestMethod]
		public void ShoudAllowNullDescription()
		{
			XmlItem item = new XmlItem("id", "title", null, new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml));

			Assert.IsNull(item.Description);
		}

		[TestMethod]
		public void ShouldEqualWithSameValues()
		{
			XmlItem i1 = new XmlItem(Guid.NewGuid().ToString(), "title", "description", new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml));
			XmlItem i2 = new XmlItem(i1.Id, "title", "description", new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), i1.Tag);

			Assert.AreEqual(i1, i2);
		}

		[TestMethod]
		public void ShouldNotEqualWithDifferentPayload()
		{
			XmlItem i1 = new XmlItem(Guid.NewGuid().ToString(), "title", "description", new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), DateTime.Now);
			XmlItem i2 = new XmlItem(i1.Id, "title", "description", new TextSyndicationContent("<payload id='foo'/>", TextSyndicationContentKind.XHtml), DateTime.Now);

			Assert.AreNotEqual(i1, i2);
		}

		[TestMethod]
		public void ShouldNotEqualWithDifferentTitle()
		{
			XmlItem i1 = new XmlItem(Guid.NewGuid().ToString(), "title1", "description", new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), DateTime.Now);
			XmlItem i2 = new XmlItem(i1.Id, "title2", "description", new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), DateTime.Now);

			Assert.AreNotEqual(i1, i2);
		}

		[TestMethod]
		public void ShouldNotEqualWithDifferentDescription()
		{
			XmlItem i1 = new XmlItem(Guid.NewGuid().ToString(), "title", "description1", new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), DateTime.Now);
			XmlItem i2 = new XmlItem(i1.Id, "title", "description2", new TextSyndicationContent("<payload/>", TextSyndicationContentKind.XHtml), DateTime.Now);

			Assert.AreNotEqual(i1, i2);
		}
	}
}
