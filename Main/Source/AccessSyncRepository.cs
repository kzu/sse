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
	public class AccessSyncRepository : DbSyncRepository
	{
		public AccessSyncRepository(Database database)
			: base(database)
		{
		}

		public AccessSyncRepository(Database database, string repositoryId)
			: base(database, repositoryId)
		{
		}

		protected override void InitializeSchema(DbConnection cn)
		{
			if (!TableExists(cn, FormatTableName("Sync")))
			{
				DbCommand cmd = cn.CreateCommand();
				cmd.CommandType = CommandType.Text;
				cmd.Connection = cn;
				cmd.CommandText = FormatSql(@"
						CREATE TABLE [{0}](
							[Id] TEXT NOT NULL PRIMARY KEY,
							[Sync] NTEXT NULL, 
							[ItemTimestamp] DATETIME NOT NULL
						)", "Sync");
				cmd.ExecuteNonQuery();
			}

			if (!TableExists(cn, FormatTableName("LastSync")))
			{
				DbCommand cmd = cn.CreateCommand();
				cmd.CommandType = CommandType.Text;
				cmd.Connection = cn;
				cmd.CommandText = FormatSql(@"
						CREATE TABLE [{0}](
							[Feed] TEXT NOT NULL PRIMARY KEY,
							[LastSync] DATETIME NOT NULL
						)", "LastSync");
				cmd.ExecuteNonQuery();
			}
		}
	}
}
