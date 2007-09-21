using System;
using System.Net;
using Microsoft.WindowsLive.Id.Client;

namespace SimpleSharing.Adapters.MSLive
{
	public class MSLiveRequest
	{
		HttpWebRequest webRequest;
		CookieContainer cookieContainer;

		public DateTime IfModifiedSince
		{
			get { return webRequest.IfModifiedSince; }
			set { webRequest.IfModifiedSince = value; }
		}

		public int Timeout
		{
			get { return webRequest.Timeout; }
			set { webRequest.Timeout = value; }
		}

		public MSLiveRequest(string uri)
		{
			Guard.ArgumentNotNullOrEmptyString(uri, "uri");

			CreateWebRequest(new Uri(uri));
		}

		public MSLiveRequest(Uri uri)
		{
			Guard.ArgumentNotNull(uri, "uri");

			CreateWebRequest(uri);
		}

		public string Method
		{
			get { return webRequest.Method; }
			set 
			{ 
				webRequest.Method = value;
				if (value.Equals("POST", StringComparison.OrdinalIgnoreCase))
				{
					webRequest.Headers.Add("X-HTTP-Method-Override", "MERGE");
				}
			}
		}

		private void CreateWebRequest(Uri uri)
		{
			webRequest = (HttpWebRequest)WebRequest.Create(uri);
			cookieContainer = new CookieContainer();
			webRequest.CookieContainer = cookieContainer;
			//webRequest.Method = "POST";
			webRequest.ContentType = "text/xml";
		}

		public System.IO.Stream GetRequestStream()
		{
			return webRequest.GetRequestStream();
		}

		public WebResponse GetResponse()
		{
			bool authenticated = false;
			RetryResponse:

			try
			{
				return webRequest.GetResponse();
			}
			catch (WebException webException)
			{
				HttpWebResponse response = webException.Response as HttpWebResponse;
				if ((response != null) &&
				(response.StatusCode == HttpStatusCode.Unauthorized))
				{
					if (!authenticated)
					{
						Authenticate();
						authenticated = true;
						goto RetryResponse;
					}
				}
				else
				{
					throw;
				}
			}
		return null;
		}

		private void Authenticate()
		{
			IdentityManager manager = IdentityManager.CreateInstance("MsLive;mslive@clariusconsulting.net;" + 
				this.GetType().Name, "Windows Live ID Client");
			Identity identity = manager.CreateIdentity();
			identity.Authenticate();

			string ticket = identity.GetTicket("mslivelabs.com", "LBI", true);
			com.mslivelabs.sse.ManageFeed mf = new com.mslivelabs.sse.ManageFeed();
			mf.CookieContainer = this.cookieContainer;

			string AuthData = " " + ticket + "&";
			mf.VerifyAuthData(AuthData);
		}
	}
}
