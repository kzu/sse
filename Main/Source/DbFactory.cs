using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

#if !PocketPC
using Microsoft.Practices.EnterpriseLibrary.Data;
#else
using Microsoft.Practices.Mobile.DataAccess;
#endif

namespace SimpleSharing
{
	public abstract class DbFactory
	{
		string connectionString;

		public DbFactory()
		{
		}

		public string ConnectionString
		{
			get { return connectionString; }
			set { connectionString = value; }
		}

		public abstract Database CreateDatabase();
	}
}
