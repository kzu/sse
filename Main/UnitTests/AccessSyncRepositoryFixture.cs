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
using System.ComponentModel;

namespace SimpleSharing.Tests
{
	[TestClass]
	public class AccessSyncRepositoryFixture : DbSyncRepositoryFixture
	{
		[TestInitialize]
		public override void Initialize()
		{
			GenericDbFactory factory = new GenericDbFactory();
			factory = new GenericDbFactory();
			factory.ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=Access2007.accdb;Persist Security Info=False";
			factory.ProviderName = "System.Data.OleDb";

			this.databaseFactory = factory;
		}

		[TestCleanup]
		public void Cleanup()
		{
			Database database = this.databaseFactory.CreateDatabase();

			using (DbConnection cn = database.CreateConnection())
			{
				cn.Open();
				DbCommand cmd = cn.CreateCommand();
				cmd.CommandText = "DROP TABLE Sync";
				try
				{
					cmd.ExecuteNonQuery();
				}
				catch { }
				cmd.CommandText = "DROP TABLE LastSync";
				try
				{
					cmd.ExecuteNonQuery();
				}
				catch { }
				cmd.CommandText = "DROP TABLE SSE_Foo_Sync";
				try
				{
					cmd.ExecuteNonQuery();
				}
				catch
				{
				}
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

		protected override DbSyncRepository CreateRepository(DbFactory databaseFactory, string repositoryId)
		{
			DbSyncRepository repo = new AccessSyncRepository();
			repo.DatabaseFactory = databaseFactory;
			repo.RepositoryId = repositoryId;

			((ISupportInitialize)repo).BeginInit();
			((ISupportInitialize)repo).EndInit();

			return repo;
		}
	}
}
