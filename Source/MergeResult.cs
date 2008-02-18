using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel.Syndication;

namespace FeedSync
{
	public class MergeResult
	{
		public MergeResult(FeedSyncSyndicationItem original, FeedSyncSyndicationItem incoming, FeedSyncSyndicationItem proposed, MergeOperation operation)
		{
			this.original = original;
			this.incoming = incoming;
			this.proposed = proposed;
			this.operation = operation;
		}

		private FeedSyncSyndicationItem original;

		public FeedSyncSyndicationItem Original
		{
			get { return original; }
		}

		private FeedSyncSyndicationItem incoming;

		public FeedSyncSyndicationItem Incoming
		{
			get { return incoming; }
		}

		private FeedSyncSyndicationItem proposed;

		public FeedSyncSyndicationItem Proposed
		{
			get { return proposed; }
		}

		private MergeOperation operation;

		public MergeOperation Operation
		{
			get { return operation; }
		}
	}
}
