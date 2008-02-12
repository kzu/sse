using System;
using System.Collections.Generic;
using System.Text;

namespace FeedSync
{
	/// <summary>
	/// Items filter
	/// </summary>
	public class ItemFilter
	{
		private Predicate<FeedSyncSyndicationItem> leftFilter;
		private Predicate<FeedSyncSyndicationItem> rightFilter;

		public ItemFilter()
		{
			this.leftFilter = NullItemFilter;
			this.rightFilter = NullItemFilter;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="commonFilter">Filter for adapters on the left and right</param>
		public ItemFilter(Predicate<FeedSyncSyndicationItem> commonFilter)
		{
			this.leftFilter = commonFilter;
			this.rightFilter = commonFilter;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="left">Filter for adapter on the left</param>
		/// <param name="right">Filter for adapter on the right</param>
		public ItemFilter(Predicate<FeedSyncSyndicationItem> left, Predicate<FeedSyncSyndicationItem> right)
		{
			this.leftFilter = left;
			this.rightFilter = right;
		}

		/// <summary>
		/// Filter for adapter on the left
		/// </summary>
		public Predicate<FeedSyncSyndicationItem> Left
		{
			get { return leftFilter; }
			set { leftFilter = value; }
		}

		/// <summary>
		/// Filter for adapter on the right
		/// </summary>
		public Predicate<FeedSyncSyndicationItem> Right
		{
			get { return rightFilter; }
			set { rightFilter = value; }
		}

		private bool NullItemFilter(FeedSyncSyndicationItem item)
		{
			return true;
		}
	}
}
