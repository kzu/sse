using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Reflection;

#if !PocketPC
using Microsoft.Practices.EnterpriseLibrary.Data;
#else
using Microsoft.Practices.Mobile.DataAccess;
#endif

namespace SimpleSharing
{
	public abstract class DbRepository
	{
		protected delegate void ExecuteDbHandler(DbConnection connection);
		
		Database database;
		bool isInitialized;
		DbFactory dbFactory;

		protected DbRepository()
		{
		}

		public void Initialize()
		{
			if (isInitialized) throw new InvalidOperationException();

			Guard.ArgumentNotNull(dbFactory, "DatabaseFactory");

			isInitialized = true;

			database = dbFactory.CreateDatabase();

			ExecuteDb(delegate(DbConnection conn)
			{
				InitializeSchema(conn);
			});
		}

		public Database Database
		{
			get
			{
				ThrowIfNotInitialized();
				return database;
			}
		}

		public DbFactory DatabaseFactory
		{
			get { return dbFactory; }
			set { dbFactory = value; }
		}

		private void ThrowIfNotInitialized()
		{
			if (!isInitialized)
				throw new InvalidOperationException(Properties.Resources.UninitializedRepository);
		}

		protected DbDataReader ExecuteReader(string sqlCommand, params DbParameter[] parameters)
		{
#if PocketPC
			return Database.ExecuteReader(sqlCommand, parameters);
#else
			DbCommand cmd = Database.DbProviderFactory.CreateCommand();
			cmd.CommandText = sqlCommand;
			cmd.Parameters.AddRange(parameters);
			DbConnection conn = Database.CreateConnection();
			cmd.Connection = conn;
			conn.Open();
			return cmd.ExecuteReader(CommandBehavior.CloseConnection);
#endif
		}

		protected int ExecuteNonQuery(string sqlCommand, params DbParameter[] parameters)
		{
#if PocketPC
			return Database.ExecuteNonQuery(sqlCommand, parameters);
#else
			DbCommand cmd = Database.DbProviderFactory.CreateCommand();
			cmd.CommandText = sqlCommand;
			cmd.CommandType = CommandType.Text;
			cmd.Parameters.AddRange(parameters);
			using (DbConnection conn = Database.CreateConnection())
			{
				cmd.Connection = conn;
				conn.Open();
				int count = cmd.ExecuteNonQuery();
				conn.Close();

				return count;
			}
#endif
		}

		protected int ExecuteNonQuery(DbCommand command, params DbParameter[] parameters)
		{
#if PocketPC
			return Database.ExecuteNonQuery(command, parameters);
#else
			command.Parameters.AddRange(parameters);
			using (DbConnection conn = Database.CreateConnection())
			{
				command.Connection = conn;
				conn.Open();
				int count = command.ExecuteNonQuery();
				conn.Close();

				return count;
			}

#endif
		}

		protected virtual DbParameter CreateParameter(string name, DbType type, int size, object value)
		{
#if PocketPC
			return Database.CreateParameter(name, type, size, value);
#else
			DbParameter param = Database.DbProviderFactory.CreateParameter();
			param.ParameterName = Database.BuildParameterName(name);
			param.DbType = type;
			param.Value = value;

			return param;
#endif
		}

		protected void ExecuteDb(ExecuteDbHandler handler)
		{
#if PocketPC
			DbConnection conn = Database.GetConnection();
			handler(conn);
#else
			using (DbConnection conn = Database.CreateConnection())
			{
				conn.Open();
				handler(conn);
				conn.Close();
			}
#endif
		}

		protected abstract void InitializeSchema(DbConnection openedConnection);

		/// <summary>
		/// Checks if the given table exist.
		/// </summary>
		protected virtual bool TableExists(DbConnection connection, string tableName)
		{
#if PocketPC
			return database.TableExists(tableName);
#else
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
#endif
		}
	}
}
