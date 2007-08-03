using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System.Data.Common;
using System.Data;

namespace SimpleSharing
{
	public abstract class DbRepository
	{
		Database database;

		protected DbRepository(Database database)
		{
			this.database = database;
			using (DbConnection cn = database.CreateConnection())
			{
				cn.Open();
				InitializeSchema(cn);
			}
		}

		protected Database Database
		{
			get { return database; }
		}

		protected DbConnection OpenConnection()
		{
			DbConnection cn = database.CreateConnection();
			cn.Open();

			return cn;
		}

		protected abstract void InitializeSchema(DbConnection openedConnection);

		/// <summary>
		/// Checks if the given table exist.
		/// </summary>
		protected virtual bool TableExists(DbConnection connection, string tableName)
		{
			// First try ADO.NET schema mechanism.
			try
			{
				DataTable userTables = connection.GetSchema("Tables");
				foreach (DataRow row in userTables.Rows)
				{
					if (tableName == row["TABLE_NAME"].ToString())
						return true;
				}

				return false;
			}
			catch (NotSupportedException)
			{
				// If it didn't work, try the ANSI SQL92 INFORMATION_SCHEMA 
				/// view on the database to query for the given table.
				using (DbCommand cmd = connection.CreateCommand())
				{
					cmd.CommandType = CommandType.Text;
					cmd.CommandText = String.Format(@"
					SELECT COUNT(*) 
					FROM	[INFORMATION_SCHEMA].[TABLES] 
					WHERE	[TABLE_NAME] = '{0}'",
							tableName);
					cmd.Connection = connection;

					int count = Convert.ToInt32(cmd.ExecuteScalar());

					return count != 0;
				}
			}
		}
	}
}
