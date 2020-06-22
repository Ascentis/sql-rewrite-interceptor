using System;
using System.Data;
using System.Data.SqlClient;

namespace Ascentis.Infrastructure
{
	public class SqlRewriteRuleDbRepository
	{
		private readonly SqlConnection _sqlConnection;
		public SqlRewriteRuleDbRepository(SqlConnection connection)
		{
			_sqlConnection = connection;
            if (_sqlConnection.State == ConnectionState.Closed)
                _sqlConnection.Open();
            EnsureSqlRewriteSchema();
		}

		private void EnsureSqlRewriteSchema()
		{
			var checkTblExists = new SqlCommand("SELECT OBJECT_ID (N'SqlRewriteRegistry', N'U') obj_id", _sqlConnection);
			var registryId = checkTblExists.ExecuteScalar();
			if (registryId == null || registryId is DBNull)
				TryCreateSqlRewriteRegistry();
		}

		private void TryCreateSqlRewriteRegistry()
		{
			var createSqlRewriteRegistryTable = new SqlCommand(@"USE [master]
				CREATE TABLE [SqlRewriteRegistry](
					[ID] [int] NOT NULL,
					[DatabaseRegEx] [nvarchar](255) NOT NULL,
					[QueryMatchRegEx] [nvarchar](max) NOT NULL,
					[QueryReplacementString] [nvarchar](max) NOT NULL,
					[RegExOptions] [int] NULL,
					CONSTRAINT [PK_SqlRewriteRegistry] PRIMARY KEY CLUSTERED 
					(
						[ID] ASC
					)
				)", _sqlConnection);
			createSqlRewriteRegistryTable.ExecuteNonQuery();
		}
	}
}
