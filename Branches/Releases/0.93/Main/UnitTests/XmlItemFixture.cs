#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System;
using System.Collections.Generic;
using System.Xml;

namespace SimpleSharing.Tests
{
	[TestClass]
	public class XmlItemFixture : TestFixtureBase
	{
		[TestMethod]
		public void ShouldGetHashcodeWithNullDescription()
		{
			XmlItem i1 = new XmlItem(Guid.NewGuid().ToString(), "title", null, DateTime.Now, GetElement("<payload/>"));
			XmlItem i2 = new XmlItem(i1.Id, "title", null, i1.Timestamp, GetElement("<payload/>"));

			Assert.AreEqual(i1, i2);
			Assert.AreEqual(i1.GetHashCode(), i2.GetHashCode());
		}

		[TestMethod]
		public void ShoudAllowNullTitle()
		{
			XmlItem item = new XmlItem(null, "description", GetElement("<payload/>"));

			Assert.IsNull(item.Title);
		}

		[TestMethod]
		public void ShoudAllowNullDescription()
		{
			XmlItem item = new XmlItem("title", null, GetElement("<payload/>"));

			Assert.IsNull(item.Description);
		}

		[TestMethod]
		public void ShouldAddEmptyPayloadIfNullPayload()
		{
			XmlItem item = new XmlItem("title", "description", null);

			Assert.IsNotNull(item.Payload);
			Assert.AreEqual(0, item.Payload.ChildNodes.Count);
		}

		[TestMethod]
		public void ShouldEqualWithSameValues()
		{
			XmlItem i1 = new XmlItem(Guid.NewGuid().ToString(), "title", "description", DateTime.Now, GetElement("<payload/>"));
			XmlItem i2 = new XmlItem(i1.Id, "title", "description", i1.Timestamp, GetElement("<payload/>"));

			Assert.AreEqual(i1, i2);
		}

		[TestMethod]
		public void ShouldNotEqualWithDifferentPayload()
		{
			XmlItem i1 = new XmlItem(Guid.NewGuid().ToString(), "title", "description", DateTime.Now, GetElement("<payload/>"));
			XmlItem i2 = new XmlItem(i1.Id, "title", "description", i1.Timestamp, GetElement("<payload id='foo'/>"));

			Assert.AreNotEqual(i1, i2);
		}

		[TestMethod]
		public void ShouldNotEqualWithDifferentTimestampSeconds()
		{
			XmlItem i1 = new XmlItem(Guid.NewGuid().ToString(), "title", "description", DateTime.Now, GetElement("<payload/>"));
			XmlItem i2 = new XmlItem(i1.Id, "title", "description", DateTime.Now.AddSeconds(50), GetElement("<payload/>"));

			Assert.AreNotEqual(i1, i2);
		}

		[TestMethod]
		public void ShouldNotEqualWithDifferentTitle()
		{
			XmlItem i1 = new XmlItem(Guid.NewGuid().ToString(), "title1", "description", DateTime.Now, GetElement("<payload/>"));
			XmlItem i2 = new XmlItem(i1.Id, "title2", "description", i1.Timestamp, GetElement("<payload/>"));

			Assert.AreNotEqual(i1, i2);
		}

		[TestMethod]
		public void ShouldNotEqualWithDifferentDescription()
		{
			XmlItem i1 = new XmlItem(Guid.NewGuid().ToString(), "title", "description1", DateTime.Now, GetElement("<payload/>"));
			XmlItem i2 = new XmlItem(i1.Id, "title", "description2", i1.Timestamp, GetElement("<payload/>"));

			Assert.AreNotEqual(i1, i2);
		}
	}
}