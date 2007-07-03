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
				cmd.CommandText = FormatSql("SELECT * FROM [{0}Sync] WHERE Id = ?");
				AddInParameter(cmd, DbType.String, id);

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
					SET Sync = ?, ItemTimestamp = ?
					WHERE Id = ?");
				AddInParameter(cmd, DbType.String, sw.ToString());
				AddInParameter(cmd, DbType.DateTime, sync.ItemTimestamp);
				AddInParameter(cmd, DbType.String, sync.Id);

				int count = cmd.ExecuteNonQuery();
				if (count == 0)
				{
					cmd.CommandText = FormatSql(@"
						INSERT INTO [{0}Sync] 
						(Sync, ItemTimestamp, Id)
						VALUES 
						(?, ?, ?)");

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
				cmd.CommandText = FormatSql("SELECT LastSync FROM [{0}LastSync] WHERE Feed = ?");
				AddInParameter(cmd, DbType.String, feed);

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
					SET LastSync = ?
					WHERE Feed = ?");
				AddInParameter(cmd, DbType.DateTime, date);
				AddInParameter(cmd, DbType.String, feed);

				int count = cmd.ExecuteNonQuery();
				if (count == 0)
				{
					cmd.CommandText = FormatSql(@"
						INSERT INTO [{0}LastSync] 
						(LastSync, Feed)
						VALUES 
						(?, ?)");

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
							[Id] NVARCHAR(300) NOT NULL PRIMARY KEY,
							[Sync] [NTEXT] NULL, 
							[ItemTimestamp] datetime NOT NULL
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
							[LastSync] [datetime] NOT NULL
						)");
				cmd.ExecuteNonQuery();
			}
		}

		private void AddInParameter(DbCommand command, DbType dbType, object value)
		{
			DbParameter param = command.CreateParameter();
			param.DbType = dbType;
			param.Value = value;
			command.Parameters.Add(param);
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

		private string FormatSql(string value)
		{
			return String.Format(value, tableNameFormat);
		}
	}
}
