using System;
using System.Collections.Generic;

namespace SimpleSharing
{
	public interface IXmlRepository
	{
		/// <summary>
		/// Returns the timestamp of the newly added item, as 
		/// generated by the underlying store.
		/// </summary>
		object Add(IXmlItem item);
		bool Contains(string id);
		IXmlItem Get(string id);
		bool Remove(string id);

		/// <summary>
		/// Updates the given item.
		/// Returns the timestamp of the updated item, as 
		/// generated by the underlying store.
		/// </summary>
		object Update(IXmlItem item);
		IEnumerable<IXmlItem> GetAll();
		IEnumerable<IXmlItem> GetAllSince(DateTime since);
	}
}
