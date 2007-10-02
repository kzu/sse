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
	public abstract partial class DbFactory
	{
		string connectionString;

		public DbFactory()
		{

		}

		public string ConnectionString
		{
			get { return connectionString; }
			set { connectionString = value; RaiseConnectionStringChanged(); }
		}

		public abstract Database CreateDatabase();

		// TODO: XamlBinding - Implement instance validation here
		private void DoValidate()
		{
			if (String.IsNullOrEmpty(connectionString))
				throw new ArgumentNullException("ConnectionString", Properties.Resources.UnitializedConnectionString);
		}
	}
}
