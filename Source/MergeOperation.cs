using System;
using System.Collections.Generic;
using System.Text;

namespace FeedSync
{
	public enum MergeOperation
	{
		Added,
		Updated, 
		Conflict,
		Removed,
		None
	}
}
