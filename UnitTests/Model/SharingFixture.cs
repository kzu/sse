using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FeedSync.Tests
{
	/// <summary>
	/// Summary description for SharingFixture
	/// </summary>
	[TestClass]
	public class SharingFixture : TestFixtureBase
	{
		public SharingFixture()
		{
		}

		[TestMethod]
		public void ShouldGetSetPublicProperties()
		{
			TestProperties(new Sharing());
		}

		
	}
}
