using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Microsoft.WindowsLive.Id.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SimpleSharing.Adapters.MSLive.Tests
{
	public class MSLiveHelper
	{
		[TestMethod]
		public void ShouldCreateAndDeleteFeed()
		{
			string url = MSLiveHelper.Create("Foo", "Bar", "http://foo");

			WebRequest wr = WebRequest.Create(url);
			Assert.AreEqual(HttpStatusCode.OK, ((HttpWebResponse)wr.GetResponse()).StatusCode);

			MSLiveHelper.Delete(url);

			wr = WebRequest.Create(url);
			try
			{
				wr.GetResponse();
				Assert.Fail("Should throw 404 error");
			}
			catch (WebException wex)
			{
				Assert.AreEqual(HttpStatusCode.NotFound, ((HttpWebResponse)wex.Response).StatusCode);
			}
		}

		static CookieContainer cookieContainer;

		static MSLiveHelper()
		{
			cookieContainer = GetCookieContainer();
		}

		/// <summary>
		/// Creates a new feed and returns the URL for the complete feed in RSS format.
		/// </summary>
		internal static string Create(string title, string description, string link)
		{
			com.mslivelabs.sse.Link linkContent = new com.mslivelabs.sse.Link();
			linkContent.Href = link;
			
			com.mslivelabs.sse.ManageFeed manageFeed = GetFeedManager();
			com.mslivelabs.sse.FeedInfo feed = manageFeed.CreateFeed(
				GetTextContent(title), 
				GetTextContent(description), 
				linkContent, null,
				com.mslivelabs.sse.FeedFormat.RSS, null);

			return feed.CompleteFeedUrl + "&alt=rss";
		}

		private static CookieContainer GetCookieContainer()
		{
			IdentityManager identityManager = IdentityManager.CreateInstance("MsLive;mslive@clariusconsulting.net;" +
				typeof(MSLiveHelper).Namespace, "Windows Live ID Client");

			Identity identity;
			try
			{
				identity = identityManager.CreateIdentity("mslive@clariusconsulting.net");
			}
			catch (WLLogOnException)
			{
				identity = identityManager.CreateIdentity();
			}

			try
			{
				identity.Authenticate(AuthenticationType.Silent);
			}
			catch (WLLogOnException)
			{
				identity.Authenticate();
			}

			string ticket = identity.GetTicket("mslivelabs.com", "LBI", true);
			com.mslivelabs.sse.ManageFeed manageFeed = new com.mslivelabs.sse.ManageFeed();
			manageFeed.CookieContainer = new CookieContainer();

			string authData = " " + ticket + "&";
			if (manageFeed.VerifyAuthData(authData))
			{
				return manageFeed.CookieContainer;
			}

			return null;
		}

		private static com.mslivelabs.sse.ManageFeed GetFeedManager()
		{
			com.mslivelabs.sse.ManageFeed manageFeed = new com.mslivelabs.sse.ManageFeed();
			manageFeed.CookieContainer = cookieContainer;
			return manageFeed;
		}

		internal static void Delete(string feedUrl)
		{
			com.mslivelabs.sse.ManageFeed manageFeed = GetFeedManager();
			manageFeed.DeleteFeed(feedUrl);
		}

		private static com.mslivelabs.sse.TextContent GetTextContent(string text)
		{
			com.mslivelabs.sse.TextContent textContent = new com.mslivelabs.sse.TextContent();
			textContent.Data = text;
			return textContent;
		}
	}
}
