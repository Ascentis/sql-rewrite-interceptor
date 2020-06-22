using System.Collections.Generic;
using System.Data.Common;

namespace Ascentis.Infrastructure
{
    public class SqlCommandRegExProcessor : SqlCommandProcessorBase
    {
        // ReSharper disable once InconsistentNaming
        private static readonly ConcurrentObjectAccessor<List<SqlRewriteRule>> _sqlRewriteRules = new ConcurrentObjectAccessor<List<SqlRewriteRule>>();
        public static SqlRewriteRule[] SqlRewriteRules
        {
            set
            {
                _sqlRewriteRules.SwapNewAndExecute(newObj =>
                {
                    foreach (var item in value)
                    {
                        newObj.Add(item);
                    }
                }, oldObj => {});
            }
        }

        public static string ProcessSqlForRegExReplacement(DbConnection dbConnection, string sqlCommand)
        {
            if (!Enabled || dbConnection == null)
                return sqlCommand;

            var sql = _sqlRewriteRules.ExecuteReadLocked(sqlRewriteRules =>
            {
                foreach (var regEx in sqlRewriteRules)
                {
                    if (!regEx.MatchDatabase(dbConnection.Database))
                        continue;
                    return regEx.ProcessQuery(sqlCommand);
                }

                return sqlCommand;
            });
            
            return sql;
        }
    }
}
