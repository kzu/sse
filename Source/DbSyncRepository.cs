using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data;
using System.IO;
using System.Xml;
using Microsoft.Practices.EnterpriseLibrary.Data;

namespace SimpleSharing
{
	public class DbSyncRepository : DbRepository, ISyncRepository
	{
		string repositoryId = String.Empty;
		string tableNameFormat;

		public DbSyncRepository(Database database)
			: this(database, null)
		{
		}

		public DbSyncRepository(Database database, string repositoryId)
			: base(database)
		{
			Guard.ArgumentNotNull(database, "database");

			if (!String.IsNullOrEmpty(repositoryId))
			{
				this.repositoryId = repositoryId;
				tableNameFormat = "SSE_" + repositoryId + "_";
			}
			else
			{
				tableNameFormat = "SSE_";
			}
		}

		public Sync Get(string id)
		{
			using (DbConnection cn = OpenConnection())
			{
				DbCommand cmd = cn.CreateCommand();
				cmd.Connection = cn;
				cmd.CommandText = FormatSql("SELECT * FROM [{0}Sync] WHERE Id = " + Database.BuildParameterName("p_id"));
				
				Database.AddInParameter(cmd, "p_id", DbType.String, id);

				DbDataReader reader = cmd.ExecuteReader();
				if (reader.Read())
				{
					return Read(reader);
				}
				else
				{
					return null;
				}
			}
		}

		public void Save(Sync sync)
		{
			Guard.ArgumentNotNull(sync.ItemTimestamp, "sync.ItemTimestamp");

			using (DbConnection cn = OpenConnection())
			{
				StringWriter sw = new StringWriter();
				using (XmlWriter xw = XmlWriter.Create(sw))
				{
					new RssFeedWriter(xw).WriteSync(sync);
				}

				DbCommand cmd = cn.CreateCommand();
				cmd.Connection = cn;
				cmd.CommandText = FormatSql(@"
					UPDATE [{0}Sync] 
					SET Sync = " + Database.BuildParameterName("p_sync") + ", ItemTimestamp = " + Database.BuildParameterName("p_timestamp") + 
								 " WHERE Id = " + Database.BuildParameterName("p_id"));

				Database.AddInParameter(cmd, "p_sync", DbType.String, sw.ToString());
				Database.AddInParameter(cmd, "p_timestamp", DbType.DateTime, sync.ItemTimestamp);
				Database.AddInParameter(cmd, "p_id", DbType.String, sync.Id);
				
				int count = cmd.ExecuteNonQuery();
				if (count == 0)
				{
					cmd.CommandText = FormatSql(@"
						INSERT INTO [{0}Sync] 
						(Sync, ItemTimestamp, Id)
						VALUES 
						(" + Database.BuildParameterName("p_sync") + ", " + Database.BuildParameterName("p_timestamp") + "," + Database.BuildParameterName("p_id") + ")");

					cmd.ExecuteNonQuery();
				}
			}
		}

		public DateTime? GetLastSync(string feed)
		{
			using (DbConnection cn = OpenConnection())
			{
				DbCommand cmd = cn.CreateCommand();
				cmd.Connection = cn;
				cmd.CommandText = FormatSql("SELECT LastSync FROM [{0}LastSync] WHERE Feed = " + Database.BuildParameterName("p_feed"));
				Database.AddInParameter(cmd, "p_feed", DbType.String, feed);
				
				DbDataReader reader = cmd.ExecuteReader();
				if (reader.Read())
				{
					return reader.GetDateTime(0);
				}
				else
				{
					return null;
				}
			}
		}

		public void SetLastSync(string feed, DateTime date)
		{
			using (DbConnection cn = OpenConnection())
			{
				DbCommand cmd = cn.CreateCommand();
				cmd.Connection = cn;
				cmd.CommandText = FormatSql(@"
					UPDATE [{0}LastSync] 
					SET LastSync = " + Database.BuildParameterName("p_lastSync") + 
									 " WHERE Feed = " + Database.BuildParameterName("p_feed"));
				
				Database.AddInParameter(cmd, "p_lastSync", DbType.DateTime, date);
				Database.AddInParameter(cmd, "p_feed", DbType.String, feed);
				
				int count = cmd.ExecuteNonQuery();
				if (count == 0)
				{
					cmd.CommandText = FormatSql(@"
						INSERT INTO [{0}LastSync] 
						(LastSync, Feed)
						VALUES 
						(" + Database.BuildParameterName("p_lastSync") + ", " + Database.BuildParameterName("p_feed") + ")");

					cmd.ExecuteNonQuery();
				}
			}
		}

		public IEnumerable<Sync> GetAll()
		{
			using (DbConnection cn = OpenConnection())
			{
				DbCommand cmd = cn.CreateCommand();
				cmd.Connection = cn;
				cmd.CommandText = FormatSql("SELECT * FROM [{0}Sync]");

				DbDataReader reader = cmd.ExecuteReader();
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
				DbCommand cmd = cn.CreateCommand();
				cmd.CommandType = CommandType.Text;
				cmd.Connection = cn;
				cmd.CommandText = FormatSql(@"
						CREATE TABLE [{0}Sync](
							[Id] NVARCHAR(254) NOT NULL PRIMARY KEY,
							[Sync] NTEXT NULL, 
							[ItemTimestamp] DATETIME NOT NULL
						)");
				cmd.ExecuteNonQuery();
			}
			
			if (!TableExists(cn, FormatTableName(repositoryId, "LastSync")))
			{
				DbCommand cmd = cn.CreateCommand();
				cmd.CommandType = CommandType.Text;
				cmd.Connection = cn;
				cmd.CommandText = FormatSql(@"
						CREATE TABLE [{0}LastSync](
							[Feed] NVARCHAR(1000) NOT NULL PRIMARY KEY,
							[LastSync] DATETIME NOT NULL
						)");
				cmd.ExecuteNonQuery();
			}
		}

		protected virtual void AddInParameter(DbCommand command, DbType dbType, object value)
		{
			DbParameter param = command.CreateParameter();
			param.DbType = dbType;
			param.Value = value;
			command.Parameters.Add(param);
		}

		protected string FormatTableName(string tableName)
		{
			return FormatTableName(repositoryId, tableName);
		}

		private static string FormatTableName(string repositoryId, string tableName)
		{
			if (!String.IsNullOrEmpty(repositoryId))
			{
				return "SSE_" + repositoryId + "_" + tableName;
			}
			else
			{
				return "SSE_" + tableName;
			}
		}

		protected string FormatSql(string value)
		{
			return String.Format(value, tableNameFormat);
		}
	}
}
