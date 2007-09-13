#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
using Microsoft.Practices.Mobile.DataAccess;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Practices.EnterpriseLibrary.Data.SqlCe;
using Microsoft.Practices.EnterpriseLibrary.Data;
#endif

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Data.SqlServerCe;
using System.IO;
using System.Data.Common;

using System.Data;

namespace SimpleSharing.Tests
{
	[TestClass]
	public class DbSyncRepositoryFixture : SyncRepositoryFixture
	{
		private delegate void ExecuteDbHandler(DbConnection connection);

        protected Database database;

		[TestInitialize]
		public virtual void Initialize()
		{
			if (File.Exists("SyncDb.sdf"))
				File.Delete("SyncDb.sdf");

			new SqlCeEngine("Data Source=SyncDb.sdf").CreateDatabase();
#if PocketPC
			this.database = new SqlDatabase("Data Source=SyncDb.sdf");
#else
			this.database = new SqlCeDatabase("Data Source=SyncDb.sdf");
#endif
		}

		[TestCleanup]
		public virtual void Cleanup()
		{
#if PocketPC
			database.GetConnection().Close();
#else
			((SqlCeDatabase)database).CloseSharedConnection();
#endif
		}

		protected virtual ISyncRepository CreateRepository(Database database, string repositoryId)
		{
			return new DbSyncRepository(database, repositoryId);
		}

		[TestMethod]
		public void ShouldAllowNullRepositoryId()
		{
			CreateRepository(database, null);
		}

		[TestMethod]
		public void ShouldAllowEmptyRepositoryId()
		{
			CreateRepository(database, "");
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfNullDatabase()
		{
			CreateRepository(null, "Foo");
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void ShouldThrowIfNullItemTimestamp()
		{
			ISyncRepository repo = CreateRepository(database, "Foo");
			Sync s = new Sync(Guid.NewGuid().ToString());

			repo.Save(s);
		}

        [TestMethod]
        public void ShouldAddSingleSyncItem()
        {
            ISyncRepository repo = CreateRepository(database, "Foo");
            Sync s = new Sync(Guid.NewGuid().ToString());
            s.ItemHash = "";

            repo.Save(s);

            int count = CountSyncRecords(s.Id);

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void ShouldModifySingleSyncItem()
        {
            ISyncRepository repo = CreateRepository(database, "Foo");
            Sync s = new Sync(Guid.NewGuid().ToString());
            s.ItemHash = "";

            repo.Save(s);

            s.ItemHash = "";
            repo.Save(s);

            int count = CountSyncRecords(s.Id);
            Assert.AreEqual(1, count);
        }

        private int CountSyncRecords(string syncId)
        {
            DbConnection connection = null;
            try
            {
#if PocketPC
                connection = database.GetConnection();
#else
                connection = database.CreateConnection();
#endif
                connection.Open();
                DbCommand command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM SSE_Foo_Sync WHERE ID = id";

                DbParameter parameter = command.CreateParameter();
                parameter.ParameterName = database.BuildParameterName("id");
                parameter.DbType = DbType.String;
                parameter.Size = 254;
                parameter.Value = syncId;

                return (int)command.ExecuteScalar();
            }
            finally
            {
#if !PocketPC
                connection.Close();
#endif
            }
        }
	}
}
