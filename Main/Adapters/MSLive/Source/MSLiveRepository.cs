using System;
using System.Collections.Generic;
using System.Text;
using SimpleSharing;
using System.Net;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.ComponentModel;
using Microsoft.Practices.EnterpriseLibrary.Validation;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace SimpleSharing.Adapters.MSLive
{
	/// <summary>
	/// Repository that exposes the SSE service on MSLiveLabs.
	/// </summary>
	public partial class MSLiveRepository : Repository
	{
		TraceSource trace = new TraceSource(typeof(MSLiveRepository).Namespace, SourceLevels.Error);
		Uri feedUrl;
		int timeoutSeconds = 60;

		/// <summary>
		/// Initializes the repository with the given feed endpoint.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="feedUrl"/> is <see langword="null" />.</exception>
		/// <exception cref="ArgumentException"><paramref name="feedUrl"/> is an empty string.</exception>
		/// <exception cref="UriFormatException"><paramref name="feedUrl"/> is not a valid <see cref="Uri"/>.</exception>
		public MSLiveRepository(string feedUrl)
		{
			Guard.ArgumentNotNullOrEmptyString(feedUrl, "feedUrl");

			this.feedUrl = new Uri(feedUrl);

			Initialize();
		}

		/// <summary>
		/// Parameterless constructor used for XAML serialization.
		/// </summary>
		public MSLiveRepository()
		{
		}

		/// <summary>
		/// Gets/Sets the <see cref="Uri"/> of the feed.
		/// </summary>
		[NotNullValidator]
		public Uri FeedUrl
		{
			get { return feedUrl; }
			set { feedUrl = value; RaiseFeedUrlChanged(); }
		}

		/// <summary>
		/// Gets/Sets the timeout in seconds for the network connection 
		/// to the service.
		/// </summary>
		public int TimeoutSeconds
		{
			get { return timeoutSeconds; }
			set { timeoutSeconds = value; RaiseTimeoutSecondsChanged(); }
		}

		public override bool SupportsMerge
		{
			get { return true; } 
		}

		public override Item Get(string id)
		{
			EnsureValid();

			Uri itemUri = new Uri(feedUrl.ToString() + "&item=" + id);
			trace.TraceInformation("Retrieving single item from url: " + itemUri.ToString());

			MSLiveRequest req = new MSLiveRequest(itemUri);
			try
			{
				using (Stream s = req.GetResponse().GetResponseStream())
				using (XmlReader r = XmlReader.Create(s))
				{
					FeedReader fr = FeedReader.Create(r);
					Feed f;
					IEnumerable<Item> items;
					fr.Read(out f, out items);
					foreach (Item item in items)
					{
						return item;
					}
				}
			}
			catch (WebException wex)
			{
				HttpWebResponse resp = wex.Response as HttpWebResponse;
				if (resp != null && resp.StatusCode == HttpStatusCode.NotFound)
				{
					trace.TraceInformation("Item with id " + id + " not found");
					return null;
				}

				trace.TraceEvent(TraceEventType.Error, 0, wex.ToString());
				throw;
			}

			return null;
		}

		public override IEnumerable<Item> GetAllSince(DateTime? since, Predicate<Item> filter)
		{
			Guard.ArgumentNotNull(filter, "filter");
			EnsureValid();

			Stream response = GetResponse(since);

			FeedReader reader = FeedReader.Create(XmlReader.Create(response));
			Feed feed;
			IEnumerable<Item> items;

			reader.Read(out feed, out items);

			return EnumerateAndDispose(items, filter, response);
		}

		public override void Add(Item item)
		{
			Update(item);
		}

		public override void Delete(string id)
		{
			throw new NotSupportedException();
		}

		public override void Update(Item item)
		{
			EnsureValid();

			MSLiveRequest req = new MSLiveRequest(feedUrl);
			req.Method = "POST";
			using (Stream s = req.GetRequestStream())
			using (XmlWriter w = XmlWriter.Create(s))
			{
				FeedWriter writer = new RssFeedWriter(w);
				writer.Write(item);
			}

			// Commit change.
			req.GetResponse().Close();
		}

		public override IEnumerable<Item> Merge(IEnumerable<Item> items)
		{
			EnsureValid();

			bool publishedItems = false;
			XmlWriterSettings fragmentSettings = new XmlWriterSettings();
			fragmentSettings.ConformanceLevel = ConformanceLevel.Fragment;
			fragmentSettings.OmitXmlDeclaration = true;
			fragmentSettings.Encoding = System.Text.Encoding.UTF8;

			MSLiveRequest req = new MSLiveRequest(feedUrl);
			req.Timeout = timeoutSeconds * 1000;
			req.Method = "POST";

			using (Stream s = req.GetRequestStream())
			using (XmlWriter writer = XmlWriter.Create(s, fragmentSettings))
			{
				RssFeedWriter feedWriter = new RssFeedWriter(writer);
				int count = 0;
				feedWriter.ItemWritten += delegate { publishedItems = true; };
				feedWriter.Write(null, items);
			}

			if (publishedItems)
			{
				req.GetResponse().Close();
			}

			// This is a costly operation as we have to get all items again.
			// This is because the service doesn't return the conflicts.
			return GetConflicts();
		}

		public override string FriendlyName
		{
			get { return feedUrl.ToString(); }
		}

		private Stream GetResponse()
		{
			return GetResponse(null);
		}

		private Stream GetResponse(DateTime? since)
		{
			MSLiveRequest req = new MSLiveRequest(feedUrl);
			req.Timeout = timeoutSeconds * 1000;

			if (since.HasValue)
			{
				req.IfModifiedSince = since.Value;
			}

			return req.GetResponse().GetResponseStream();
		}

		private IEnumerable<Item> EnumerateAndDispose(IEnumerable<Item> items, Predicate<Item> filter, params IDisposable[] disposables)
		{
			foreach (Item item in items)
			{
				if (filter(item))
					yield return item;
			}

			foreach (IDisposable disposable in disposables)
			{
				disposable.Dispose();
			}
		}

		// TODO: XamlBinding - Implement instance validation here
		private void DoValidate()
		{
			Validation.ValidateThrow(this);
		}
	}
}
