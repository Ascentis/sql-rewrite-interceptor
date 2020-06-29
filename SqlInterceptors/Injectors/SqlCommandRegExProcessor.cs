using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Ascentis.Infrastructure.SqlInterceptors.Model;
using Ascentis.Infrastructure.SqlInterceptors.Properties;
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace Ascentis.Infrastructure.SqlInterceptors.Injectors
{
    public static class SqlCommandRegExProcessor
    {
        public const string RegReplacementIndicator = "/*x*/";
        public static bool RegExInjectionEnabled = Settings.Default.RegExInjectionEnabled;

        // ReSharper disable once InconsistentNaming
        private static readonly ConcurrentObjectAccessor<List<SqlRewriteRule>> _sqlRewriteRules = new ConcurrentObjectAccessor<List<SqlRewriteRule>>();

        public static IEnumerable<SqlRewriteRule> SqlRewriteRules
        {
            set
            {
                _sqlRewriteRules.SwapNewAndExecute(newObj => { newObj.AddRange(value); }, oldObj => { });
            }
        }

        public static string ProcessSqlForRegExReplacement(DbConnection dbConnection, SqlCommand sqlCmd, string sqlCommand, CommandType commandType)
        {
            try
            {
                if (!RegExInjectionEnabled || !SqlCommandInterceptor.Enabled || dbConnection == null || sqlCommand.EndsWith(RegReplacementIndicator))
                    return sqlCommand;
                var sql = _sqlRewriteRules.ExecuteReadLocked(sqlRewriteRules =>
                {
                    foreach (var regEx in sqlRewriteRules)
                    {
                        if (!regEx.MatchDatabase(dbConnection.Database))
                            continue;
                        var newCommand = regEx.ProcessQuery(sqlCommand);
                        if (newCommand.EndsWith(RegReplacementIndicator) || newCommand != sqlCommand)
                            return newCommand;
                    }

                    return sqlCommand;
                });

                return sql;
            }
            catch (Exception e)
            {
                SqlCommandInterceptor.ExceptionDelegateEvent?.Invoke(e);
                return sqlCommand;
            }
        }
    }
}