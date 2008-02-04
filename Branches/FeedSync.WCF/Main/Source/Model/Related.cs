using System;
using System.Collections.Generic;
using System.Text;

namespace FeedSync
{
	public class Related : ICloneable<Related>
	{
		private string link;
		private string title;
		private RelatedType type;

		public Related(string linkUrl, RelatedType type)
			: this(linkUrl, type, null)
		{
		}

		public Related(string linkUrl, RelatedType type, string title)
		{
			this.link = linkUrl;
			this.type = type;
			this.title = title;
		}

		public string Link
		{
			get { return link; }
		}

		public string Title
		{
			get { return title; }
		}

		public RelatedType Type
		{
			get { return type; }
		}

		#region ICloneable<Related> Members

		public Related Clone()
		{
			return new Related(link, type, title);
		}

		#endregion

		#region ICloneable Members

		object ICloneable.Clone()
		{
			return Clone();
		}

		#endregion
	}
}
