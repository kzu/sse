using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;

namespace FeedSync
{
	[Obsolete("Use IRepository interface directly")]
	public interface IXmlRepository
	{
		void Add(XmlItem item, out object tag);
		bool Contains(string id);
		XmlItem Get(string id);
		bool Remove(string id);

		void Update(XmlItem item, out object tag);
		IEnumerable<XmlItem> GetAll();
		IEnumerable<XmlItem> GetAllSince(DateTime since);
	}
}
