using System.Collections.Generic;
using Ascentis.Infrastructure.SqlInterceptors.Model;

namespace Ascentis.Infrastructure.SqlInterceptors.Repository
{
    public interface ISqlRewriteRepository
    {
        void SaveSqlRewriteRule(SqlRewriteRule rule);
        void RemoveSqlRewriteRule(int id);
        IEnumerable<SqlRewriteRule> LoadSqlRewriteRules();
        void SaveSqlRewriteSettings(SqlRewriteSettings settings);
        IEnumerable<SqlRewriteSettings> LoadSqlRewriteSettings();
        void RemoveSqlRewriteSettings(int id);
        void RemoveAllSqlRewriteSettings();
        bool IsThreadSafe();
    }
}
