using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.IO;
using System.Xml;

namespace SimpleSharing.Adapters.MSLive.Tests
{
	[TestClass]
	public class MSLiveHttpWebRequestFixture
	{
		string url;

		[ClassInitialize]
		public void Setup()
		{
			url = MSLiveHelper.Create("foo", "bar", "http://foobar");
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfNullUrl()
		{
			new MSLiveRequest((String)null);
		}

		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void ShouldThrowIfEmptyUrl()
		{
			new MSLiveRequest(String.Empty);
		}

		[TestMethod]
		public void ShouldSetIfModifiedSince()
		{
			MSLiveRequest req = new MSLiveRequest(new Uri("http://www.foo.com"));
			DateTime modifiedSince = DateTime.Now;
			
			req.IfModifiedSince = modifiedSince;

			Assert.AreEqual(modifiedSince, req.IfModifiedSince);
		}

		[TestMethod]
		public void ShouldSetTimeout()
		{
			MSLiveRequest req = new MSLiveRequest(new Uri("http://www.foo.com"));
			int timeOut = 17;

			req.Timeout = timeOut;

			Assert.AreEqual(timeOut, req.Timeout); 
		}

		[TestMethod]
		public void ShouldGetResponse()
		{
			MSLiveRequest req = new MSLiveRequest("http://sse.mslivelabs.com/");

			WebResponse resp = req.GetResponse();

			Assert.IsNotNull(resp);
		}

		// Test ShouldGetAfterIfModifiedSince

	}
}
