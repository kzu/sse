#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System;
using System.Collections.Generic;
using System.Threading;

namespace SimpleSharing
{
	[TestClass]
	public class CompoundRepositoryFixture
	{



		public class CompoundRepository : IRepository
		{

			#region IRepository Members

			public bool SupportsMerge
			{
				get { throw new Exception("The method or operation is not implemented."); }
			}

			public Item Get(string id)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			public IEnumerable<Item> GetAll()
			{
				throw new Exception("The method or operation is not implemented.");
			}

			public IEnumerable<Item> GetAllSince(DateTime? since)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			public void Add(Item item)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			public void Delete(string id)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			public void Update(Item item)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			public IList<Item> Merge(IEnumerable<Item> items)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			public string FriendlyName
			{
				get { throw new Exception("The method or operation is not implemented."); }
			}

			#endregion
		}
	}
}
