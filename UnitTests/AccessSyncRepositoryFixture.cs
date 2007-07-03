#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Data.SqlServerCe;
using System.IO;
using System.Data.Common;
using Microsoft.Practices.EnterpriseLibrary.Data.SqlCe;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System.Data;

namespace SimpleSharing.Tests
{
	[TestClass]
	public class AccessSyncRepositoryFixture : DbSyncRepositoryFixture
	{
		[TestInitialize]
		public override void Initialize()
		{
			database = new GenericDatabase(
				"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=Access2007.accdb;Persist Security Info=False", 
				DbProviderFactories.GetFactory("System.Data.OleDb"));
		}

		[TestCleanup]
		public override void Cleanup()
		{
			using (DbConnection cn = database.CreateConnection())
			{
				cn.Open();
				DbCommand cmd = cn.CreateCommand();
				cmd.CommandText = "DROP TABLE SSE_Foo_Sync";
				try
				{
					cmd.ExecuteNonQuery();
				}
				catch { }
				cmd.CommandText = "DROP TABLE SSE_Foo_LastSync";
				try
				{
					cmd.ExecuteNonQuery();
				}
				catch { }
				cmd.CommandText = "DROP TABLE SSE_Sync";
				try
				{
					cmd.ExecuteNonQuery();
				}
				catch { }
				cmd.CommandText = "DROP TABLE SSE_LastSync";
				try
				{
					cmd.ExecuteNonQuery();
				}
				catch { }
			}
		}

		protected override ISyncRepository CreateRepository(Database database, string repositoryId)
		{
			return new AccessSyncRepository(database, repositoryId);
		}
	}
}
