using System;
using System.Collections.Generic;
using System.Data.Common;
using SqlInterceptor.Properties;

namespace Ascentis.Infrastructure
{
    public class SqlCommandRegExProcessor
    {
        public static bool RegExInjectionEnabled = Settings.Default.RegExInjectionEnabled;

        // ReSharper disable once InconsistentNaming
        private static readonly ConcurrentObjectAccessor<List<SqlRewriteRule>> _sqlRewriteRules = new ConcurrentObjectAccessor<List<SqlRewriteRule>>();

        public static IEnumerable<SqlRewriteRule> SqlRewriteRules
        {
            set
            {
                _sqlRewriteRules.SwapNewAndExecute(newObj =>
                {
                    foreach (var item in value)
                        newObj.Add(item);
                }, oldObj => { });
            }
        }

        public static string ProcessSqlForRegExReplacement(DbConnection dbConnection, string sqlCommand)
        {
            if (!RegExInjectionEnabled || !SqlCommandProcessor.Enabled || dbConnection == null)
                return sqlCommand;
            try
            {
                var sql = _sqlRewriteRules.ExecuteReadLocked(sqlRewriteRules =>
                {
                    foreach (var regEx in sqlRewriteRules)
                    {
                        if (!regEx.MatchDatabase(dbConnection.Database))
                            continue;
                        var newCommand = regEx.ProcessQuery(sqlCommand);
                        if (newCommand != sqlCommand)
                            return newCommand;
                    }

                    return sqlCommand;
                });

                return sql;
            }
            catch (Exception e)
            {
                SqlCommandInterceptor.ExceptionDelegateEvent?.Invoke(e);
                RegExInjectionEnabled = false;
                return sqlCommand;
            }
        }
    }
}