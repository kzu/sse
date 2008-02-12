using System;
using System.Collections.Generic;
using System.Text;

namespace FeedSync
{
	public class ItemEventArgs : EventArgs
	{
		private FeedSyncSyndicationItem item;

		public ItemEventArgs(FeedSyncSyndicationItem item)
		{
			Guard.ArgumentNotNull(item, "item");

			this.item = item;
		}

		public FeedSyncSyndicationItem Item
		{
			get { return item; }
		}
	}
}
