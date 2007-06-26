using System;
using System.Collections.Generic;

namespace SimpleSharing
{
	public interface IFeedReader
	{
		event EventHandler ItemRead;
		void Read(out Feed feed, out IEnumerable<Item> items);
	}
}
