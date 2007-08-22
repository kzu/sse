using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data;
using System.IO;
using System.Xml;
#if !PocketPC
using Microsoft.Practices.EnterpriseLibrary.Data;
#else
using Microsoft.Practices.Mobile.DataAccess;
#endif

namespace SimpleSharing
{
	public class DbSyncRepository : DbRepository, ISyncRepository
	{
		private const string RepositoryPrefix = "SSE_";
		string repositoryId;

		public DbSyncRepository(Database database)
			: this(database, null)
		{
		}

		public DbSyncRepository(Database database, string repositoryId)
			: base(database)
		{
			Guard.ArgumentNotNull(database, "database");

			this.repositoryId = repositoryId;
			Initialize();
		}

		public Sync Get(string id)
		{
			using (DbDataReader reader = ExecuteReader(
				FormatSql(@"SELECT * FROM [{0}] WHERE Id = {1}", "Sync", "id"),
				CreateParameter("id", DbType.String, 254, id)))
			{
				if (reader.Read())
					return Read(reader);

				return null;
			}
		}

		public void Save(Sync sync)
		{
			Guard.ArgumentNotNull(sync.ItemTimestamp, "sync.ItemTimestamp");

			string data = Write(sync);

			ExecuteDb(delegate(DbConnection conn)
			{
				using (DbTransaction transaction = conn.BeginTransaction())
				{
					using (DbCommand cmd = conn.CreateCommand())
					{
						cmd.CommandText = FormatSql(@"
							UPDATE [{0}] 
								SET Sync = {2}, ItemTimestamp = {3}
							WHERE Id = {1}", "Sync", "id", "sync", "ts");

						int count = ExecuteNonQuery(cmd,
							CreateParameter("id", DbType.String, 254, sync.Id),
							CreateParameter("sync", DbType.String, 0, data),
							CreateParameter("ts", DbType.DateTime, 0, sync.ItemTimestamp));
						if (count == 0)
						{
							cmd.CommandText = FormatSql(@"
								INSERT INTO [{0}] (Id, Sync, ItemTimestamp)
								VALUES ({1}, {2}, {3})", "Sync", "id", "sync", "ts");
							// The parameters are already set on the command
							count = this.Database.ExecuteNonQuery(cmd);
						}
					}
					transaction.Commit();
				}
			});
		}

		public DateTime? GetLastSync(string feed)
		{
			using (DbDataReader reader = ExecuteReader(
				FormatSql(@"SELECT LastSync FROM [{0}] WHERE Feed = {1}", "LastSync", "feed"),
				CreateParameter("feed", DbType.String, 1000, feed)))
			{
				if (reader.Read())
					return reader.GetDateTime(0);

				return null;
			}
		}

		public void SetLastSync(string feed, DateTime date)
		{
			ExecuteDb(delegate(DbConnection conn)
			{
				using (DbTransaction transaction = conn.BeginTransaction())
				{
					using (DbCommand cmd = conn.CreateCommand())
					{
						cmd.CommandText = FormatSql(@"
							UPDATE [{0}] 
								SET LastSync = {1}
							WHERE Feed = {2}", "LastSync", "date", "feed");

						int count = ExecuteNonQuery(cmd,
							CreateParameter("date", DbType.DateTime, 0, date),
							CreateParameter("feed", DbType.String, 1000, feed));
						if (count == 0)
						{
							cmd.CommandText = FormatSql(@"
								INSERT INTO [{0}] (LastSync, Feed)
								VALUES ({1}, {2})", "LastSync", "date", "feed");
							// The parameters are already set on the command
							count = ExecuteNonQuery(cmd);
						}

						transaction.Commit();
					}
				}
			});
		}

		public IEnumerable<Sync> GetAll()
		{
			using (DbDataReader reader = ExecuteReader(FormatSql("SELECT * FROM [{0}]", "Sync")))
			{
				while (reader.Read())
				{
					yield return Read(reader);
				}
			}
		}

		public IEnumerable<Sync> GetConflicts()
		{
			// TODO: sub-optimal.
			foreach (Sync sync in GetAll())
			{
				if (sync.Conflicts.Count > 0)
					yield return sync;
			}
		}

		private Sync Read(DbDataReader reader)
		{
			string xml = (string)reader["Sync"];
			XmlReader xr = XmlReader.Create(new StringReader(xml));
			xr.MoveToContent();

			Sync sync = new FeedReader.SyncXmlReader(xr, new RssFeedReader(xr)).ReadSync();
			sync.ItemTimestamp = (DateTime)reader["ItemTimestamp"];

			return sync;
		}

		protected override void InitializeSchema(DbConnection cn)
		{
			if (!TableExists(cn, FormatTableName(repositoryId, "Sync")))
			{
				ExecuteNonQuery(FormatSql(@"
						CREATE TABLE [{0}](
							[Id] NVARCHAR(254) NOT NULL PRIMARY KEY,
							[Sync] NTEXT NULL, 
							[ItemTimestamp] DATETIME NOT NULL
						)", "Sync"));
			}

			if (!TableExists(cn, FormatTableName(repositoryId, "LastSync")))
			{
				ExecuteNonQuery(FormatSql(@"
						CREATE TABLE [{0}](
							[Feed] NVARCHAR(1000) NOT NULL PRIMARY KEY,
							[LastSync] DATETIME NOT NULL
						)", "LastSync"));
			}
		}

		protected string FormatTableName(string tableName)
		{
			return FormatTableName(repositoryId, tableName);
		}

		private static string FormatTableName(string repositoryId, string tableName)
		{
			if (!String.IsNullOrEmpty(repositoryId))
			{
				return RepositoryPrefix + repositoryId + "_" + tableName;
			}
			else
			{
				return RepositoryPrefix + tableName;
			}
		}

		protected string FormatSql(string cmd, string tableName, params string[] parms)
		{
			string[] names = new string[1 + (parms != null ? parms.Length : 0)];
			names[0] = FormatTableName(tableName);
			if (parms != null)
			{
				int index = 1;
				foreach (string parm in parms)
					names[index++] = this.Database.BuildParameterName(parm);
			}
			return String.Format(cmd, names);
		}

		private string Write(Sync sync)
		{
			StringWriter sw = new StringWriter();
			using (XmlWriter xw = XmlWriter.Create(sw))
			{
				new RssFeedWriter(xw).WriteSync(sync);
			}
			return sw.ToString();
		}
	}
}
