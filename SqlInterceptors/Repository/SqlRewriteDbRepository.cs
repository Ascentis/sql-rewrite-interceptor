using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Ascentis.Infrastructure.SqlInterceptors.Model;
using Ascentis.Infrastructure.SqlInterceptors.Repository;

namespace Ascentis.Infrastructure.DBRepository
{
    public class SqlRewriteDbRepository : IDisposable, ISqlRewriteRepository
    {
        private readonly bool _connectionOwned;
        private readonly SqlConnection _sqlConnection;
        public SqlRewriteDbRepository(SqlConnection connection)
        {
            _sqlConnection = connection;
            if (_sqlConnection.State == ConnectionState.Closed)
                _sqlConnection.Open();
            EnsureSqlRewriteSchema();
        }

        public SqlRewriteDbRepository(string connectionString) : this(new SqlConnection(connectionString))
        {
            _connectionOwned = true;
        }

        private void EnsureSqlRewriteSchema()
        {
            using var checkTblExists = new SqlCommand("SELECT OBJECT_ID (N'SqlRewriteRegistry', N'U') obj_id", _sqlConnection);
            var registryId = checkTblExists.ExecuteScalar();
            if (registryId == null || registryId is DBNull)
                TryCreateSqlRewriteSchema();
        }

        private void TryCreateSqlRewriteSchema()
        {
            using var createSqlRewriteRegistryTable = new SqlCommand(@"
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
                )
                CREATE TABLE [dbo].[SqlRewriteInjectorSettings](
                    [Id] [int] IDENTITY(1,1) NOT NULL,
                    [MachineRegEx] [varchar](max) NOT NULL,
                    [ProcessNameRegEx] [varchar](max) NOT NULL,
                    [Enabled] [bit] NOT NULL,
                    [HashInjectionEnabled] [bit] NOT NULL,
                    [RegExInjectionEnabled] [bit] NOT NULL,
                    [StackFrameInjectionEnabled] [bit] NOT NULL,
                    [CallStackEntriesToReport] [int] NOT NULL
                 CONSTRAINT [PK_SqlRewriteInjectorSettings] PRIMARY KEY CLUSTERED 
                    (
                        [Id] ASC
                    )
                );
                ALTER TABLE [dbo].[SqlRewriteInjectorSettings] ADD  CONSTRAINT [DF_SqlRewriteInjectorSettings_Enabled]  DEFAULT ((0)) FOR [Enabled];
                ALTER TABLE [dbo].[SqlRewriteInjectorSettings] ADD  CONSTRAINT [DF_SqlRewriteInjectorSettings_HashInjectionEnabled]  DEFAULT ((1)) FOR [HashInjectionEnabled];
                ALTER TABLE [dbo].[SqlRewriteInjectorSettings] ADD  CONSTRAINT [DF_SqlRewriteInjectorSettings_RegExInjectionEnabled]  DEFAULT ((0)) FOR [RegExInjectionEnabled];
                ALTER TABLE [dbo].[SqlRewriteInjectorSettings] ADD  CONSTRAINT [DF_SqlRewriteInjectorSettings_StackFrameInjectionEnabled]  DEFAULT ((0)) FOR [StackFrameInjectionEnabled];
                ALTER TABLE [dbo].[SqlRewriteInjectorSettings] ADD  CONSTRAINT [DF_SqlRewriteInjectorSettings_CallStackEntriesToReport]  DEFAULT ((10)) FOR [CallStackEntriesToReport];", _sqlConnection);
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

        public void SaveSqlRewriteRule(SqlRewriteRule rule)
        {
            using var saveSqlRewriteRule = new SqlCommand(@"
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
                OUTPUT Inserted.ID;", _sqlConnection);
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

        public void RemoveSqlRewriteRule(int id)
        {
            using var deleteSqlRewriteRule = new SqlCommand(@"
                DELETE FROM SqlRewriteRegistry
                WHERE ID = @ID", _sqlConnection);
            deleteSqlRewriteRule.Parameters.AddWithValue("@ID", id);
            if(deleteSqlRewriteRule.ExecuteNonQuery() != 1)
                throw new SqlRewriteRuleDbRepositoryException("No sql rewrite entry was found to remove");
        }

        public IEnumerable<SqlRewriteRule> LoadSqlRewriteRules()
        {
            var result = new List<SqlRewriteRule>();
            using var loadSqlRewriteRules = new SqlCommand(@"
                SELECT ID, DatabaseRegEx, QueryMatchRegEx, QueryReplacementString, RegExOptions
                FROM SqlRewriteRegistry", _sqlConnection);
            using var resultSet = loadSqlRewriteRules.ExecuteReader();
            while (resultSet.Read())
            {
                var item = new SqlRewriteRule
                {
                    Id = (int) resultSet[0],
                    DatabaseRegEx = (string) resultSet[1],
                    QueryMatchRegEx = (string) resultSet[2],
                    QueryReplacementString = (string) resultSet[3],
                    RegExOptions = (RegexOptions) resultSet[4]
                };
                result.Add(item);
            }

            return result;
        }

        public void SaveSqlRewriteSettings(SqlRewriteSettings settings)
        {
            using var saveSqlRewriteSettings = new SqlCommand(@"
                MERGE SqlRewriteInjectorSettings AS t
                USING (
                    SELECT @ID, @MachineRegEx, @ProcessNameRegEx, @Enabled, @HashInjectionEnabled, @RegExInjectionEnabled, @StackFrameInjectionEnabled, @CallStackEntriesToReport
                ) AS src (ID, MachineRegEx, ProcessNameRegEx, Enabled, HashInjectionEnabled, RegExInjectionEnabled, StackFrameInjectionEnabled, CallStackEntriesToReport)
                ON (t.ID = src.ID)
                WHEN MATCHED THEN
                    UPDATE SET 
                        t.MachineRegEx = src.MachineRegEx,
                        t.ProcessNameRegEx = src.ProcessNameRegEx,
                        t.Enabled = src.Enabled,
                        t.HashInjectionEnabled = src.HashInjectionEnabled,
                        t.RegExInjectionEnabled = src.RegExInjectionEnabled,
                        t.StackFrameInjectionEnabled = src.StackFrameInjectionEnabled,
                        t.CallStackEntriesToReport = src.CallStackEntriesToReport
                WHEN NOT MATCHED THEN
                    INSERT (MachineRegEx, ProcessNameRegEx, Enabled, HashInjectionEnabled, RegExInjectionEnabled, StackFrameInjectionEnabled, CallStackEntriesToReport)
                    VALUES (src.MachineRegEx, src.ProcessNameRegEx, src.Enabled, src.HashInjectionEnabled, src.RegExInjectionEnabled, src.StackFrameInjectionEnabled, src.CallStackEntriesToReport)
                OUTPUT Inserted.ID;", _sqlConnection);
            saveSqlRewriteSettings.Parameters.AddRange(new[]
            {
                new SqlParameter("@ID", settings.Id),
                new SqlParameter("@MachineRegEx", settings.MachineRegEx),
                new SqlParameter("@ProcessNameRegEx", settings.ProcessNameRegEx),
                new SqlParameter("@Enabled", settings.Enabled ? 1 : 0),
                new SqlParameter("@HashInjectionEnabled", settings.HashInjectionEnabled ? 1 : 0),
                new SqlParameter("@StackFrameInjectionEnabled", settings.StackFrameInjectionEnabled ? 1 : 0),
                new SqlParameter("@RegExInjectionEnabled", settings.RegExInjectionEnabled ? 1 : 0),
                new SqlParameter("@CallStackEntriesToReport", settings.CallStackEntriesToReport), 
            });
            var result = saveSqlRewriteSettings.ExecuteScalar();
            if (result is DBNull)
                throw new SqlRewriteRuleDbRepositoryException("Call to saveSqlRewriteSettings.ExecuteScalar() should always return a non-null value");
            settings.Id = (int)result;
        }

        public IEnumerable<SqlRewriteSettings> LoadSqlRewriteSettings()
        {
            var result = new List<SqlRewriteSettings>();
            using var loadSqlRewriteSettings = new SqlCommand(@"
                SELECT ID, MachineRegEx, ProcessNameRegEx, Enabled, HashInjectionEnabled, RegExInjectionEnabled, StackFrameInjectionEnabled, CallStackEntriesToReport
                FROM SqlRewriteInjectorSettings", _sqlConnection);
            using var resultSet = loadSqlRewriteSettings.ExecuteReader();
            while (resultSet.Read())
            {
                var item = new SqlRewriteSettings
                {
                    Id = (int)resultSet[0],
                    MachineRegEx = (string)resultSet[1],
                    ProcessNameRegEx = (string)resultSet[2],
                    Enabled = (bool)resultSet[3],
                    HashInjectionEnabled = (bool)resultSet[4],
                    RegExInjectionEnabled = (bool)resultSet[5],
                    StackFrameInjectionEnabled = (bool)resultSet[6],
                    CallStackEntriesToReport = (int)resultSet[7]
                };
                result.Add(item);
            }

            return result;
        }

        public void RemoveSqlRewriteSettings(int id)
        {
            using var deleteSqlRewriteRule = new SqlCommand(@"
                DELETE FROM SqlRewriteInjectorSettings
                WHERE ID = @ID", _sqlConnection);
            deleteSqlRewriteRule.Parameters.AddWithValue("@ID", id);
            if (deleteSqlRewriteRule.ExecuteNonQuery() != 1)
                throw new SqlRewriteRuleDbRepositoryException("No settings entry was found to remove");
        }

        public void RemoveAllSqlRewriteSettings()
        {
            using var truncateSqlRewriteInjectorSettings = new SqlCommand("TRUNCATE TABLE SqlRewriteInjectorSettings", _sqlConnection);
            truncateSqlRewriteInjectorSettings.ExecuteNonQuery();
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
