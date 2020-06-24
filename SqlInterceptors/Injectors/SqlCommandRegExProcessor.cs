using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Ascentis.Infrastructure.Properties;

namespace Ascentis.Infrastructure
{
    public class SqlCommandRegExProcessor
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

        public static string ProcessSqlForRegExReplacement(DbConnection dbConnection, string sqlCommand, CommandType commandType)
        {
            if (!RegExInjectionEnabled || !SqlCommandProcessor.Enabled || dbConnection == null || sqlCommand.EndsWith(RegReplacementIndicator))
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
                RegExInjectionEnabled = false;
                return sqlCommand;
            }
        }
    }
}