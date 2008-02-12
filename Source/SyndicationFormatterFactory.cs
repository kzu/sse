using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Syndication;

namespace FeedSync
{
	public class SyndicationFormatterFactory
	{
		/// <summary>
		/// Creates a <see cref="SyndicationItemFormatter"/> according to the 
		/// input format.
		/// </summary>
		public static SyndicationItemFormatter CreateItemFormatter(string version, FeedSyncSyndicationItem item)
		{
			Guard.ArgumentNotNullOrEmptyString(version, "version");

			if(version == "Rss20")
				return new Rss20ItemFormatter<FeedSyncSyndicationItem>(item);
			else if(version == "Atom10")
				return new Atom10ItemFormatter<FeedSyncSyndicationItem>(item);
			
			throw new NotSupportedException(Properties.Resources.NotSupportedFormatter);
		}

		/// <summary>
		/// Creates a <see cref="SyndicationItemFormatter"/> according to the 
		/// input format.
		/// </summary>
		public static SyndicationItemFormatter CreateItemFormatter(string version)
		{
			if (version == "Rss20")
				return new Rss20ItemFormatter<FeedSyncSyndicationItem>();
			else if (version == "Atom10")
				return new Atom10ItemFormatter<FeedSyncSyndicationItem>();

			throw new NotSupportedException(Properties.Resources.NotSupportedFormatter);
		}
	}
}
