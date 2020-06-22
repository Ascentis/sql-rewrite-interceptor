using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Ascentis.Infrastructure
{
	public class SqlRewriteRuleDbRepository : IDisposable, ISqlRewriteRuleRepository
    {
        private readonly bool _connectionOwned;
        private readonly SqlConnection _sqlConnection;
		public SqlRewriteRuleDbRepository(SqlConnection connection)
		{
			_sqlConnection = connection;
			if (_sqlConnection.State == ConnectionState.Closed)
				_sqlConnection.Open();
			EnsureSqlRewriteSchema();
		}

        public SqlRewriteRuleDbRepository(string connectionString) : this(new SqlConnection(connectionString))
        {
            _connectionOwned = true;
        }

        private void EnsureSqlRewriteSchema()
		{
			using (var checkTblExists =
				new SqlCommand("SELECT OBJECT_ID (N'SqlRewriteRegistry', N'U') obj_id", _sqlConnection))
			{
				var registryId = checkTblExists.ExecuteScalar();
				if (registryId == null || registryId is DBNull)
					TryCreateSqlRewriteRegistry();
			}
		}

		private void TryCreateSqlRewriteRegistry()
		{
			using (var createSqlRewriteRegistryTable = new SqlCommand(@"
				CREATE TABLE [SqlRewriteRegistry](
					[ID] [int] IDENTITY(1,1) NOT NULL,
					[DatabaseRegEx] [nvarchar](255) NOT NULL,
					[QueryMatchRegEx] [nvarchar](max) NOT NULL,
					[QueryReplacementString] [nvarchar](max) NOT NULL,
					[RegExOptions] [int] NULL,
					CONSTRAINT [PK_SqlRewriteRegistry] PRIMARY KEY CLUSTERED 
					(
						[ID] ASC
					)
				)", _sqlConnection))
			{
                try
                {
                    createSqlRewriteRegistryTable.ExecuteNonQuery();
                }
                catch (SqlException e)
                {
                    if (!e.Message.Contains("There is already an object"))
                        throw;
                }
            }
		}

		public void Save(SqlRewriteRule rule)
		{
			using (var saveSqlRewriteRule = new SqlCommand(@"
				MERGE SqlRewriteRegistry AS t
				USING (
					SELECT @ID, @DatabaseRegEx, @QueryMatchRegEx, @QueryReplacementString, @RegExOptions
				) AS src (ID, DatabaseRegEx, QueryMatchRegEx, QueryReplacementString, RegExOptions)
				ON (t.ID = src.ID)
				WHEN MATCHED THEN
					UPDATE SET 
						t.DatabaseRegEx = src.DatabaseRegEx,
		         		t.QueryMatchRegEx = src.QueryMatchRegEx,
						t.QueryReplacementString = src.QueryReplacementString,
						t.RegExOptions = src.RegExOptions
				WHEN NOT MATCHED THEN
					INSERT (DatabaseRegEx, QueryMatchRegEx, QueryReplacementString, RegExOptions)
					VALUES (src.DatabaseRegEx, src.QueryMatchRegEx, src.QueryReplacementString, src.RegExOptions)
				OUTPUT Inserted.ID;", _sqlConnection))
			{
				saveSqlRewriteRule.Parameters.AddRange(new[]
				{
					new SqlParameter("@ID", rule.Id),
					new SqlParameter("@DatabaseRegEx", rule.DatabaseRegEx),
					new SqlParameter("@QueryMatchRegEx", rule.QueryMatchRegEx),
					new SqlParameter("@QueryReplacementString", rule.QueryReplacementString),
					new SqlParameter("@RegExOptions", rule.RegExOptions)
				});
				var result = saveSqlRewriteRule.ExecuteScalar();
				if (result is DBNull)
					throw new SqlRewriteRuleDbRepositoryException("Call to saveSqlRewriteRule.ExecuteScalar() should always return a non-null value");
				rule.Id = (int) result;
			}
		}

		public void Remove(int id)
		{
			using (var deleteSqlRewriteRule = new SqlCommand(@"
				DELETE FROM SqlRewriteRegistry
				WHERE ID = @ID", _sqlConnection))
			{
				deleteSqlRewriteRule.Parameters.AddWithValue("@ID", id);
				deleteSqlRewriteRule.ExecuteScalar();
			}
		}

		public IEnumerable<SqlRewriteRule> Load()
		{
			var loadSqlRewriteRules = new SqlCommand(@"
				SELECT ID, DatabaseRegEx, QueryMatchRegEx, QueryReplacementString, RegExOptions
				FROM SqlRewriteRegistry", _sqlConnection);
			var result = new List<SqlRewriteRule>();
			using (var resultSet = loadSqlRewriteRules.ExecuteReader())
			{
				while (resultSet.Read())
				{
					var item = new SqlRewriteRule
					{
						Id = (int)resultSet[0],
						DatabaseRegEx = (string) resultSet[1],
						QueryMatchRegEx = (string) resultSet[2],
						QueryReplacementString = (string) resultSet[3],
						RegExOptions = (RegexOptions)resultSet[4]
					};
					result.Add(item);
				}
			}
			return result;
		}

        public bool IsThreadSafe()
        {
            return _connectionOwned;
        }

        public void Dispose()
        {
            if (_connectionOwned)
                _sqlConnection.Dispose();
        }
    }
}
