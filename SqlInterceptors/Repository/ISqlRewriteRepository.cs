using System.Collections.Generic;

namespace Ascentis.Infrastructure
{
    public interface ISqlRewriteRepository
    {
        void SaveSqlRewriteRule(SqlRewriteRule rule);
        void RemoveSqlRewriteRule(int id);
        IEnumerable<SqlRewriteRule> LoadSqlRewriteRules();
        bool IsThreadSafe();
        void SaveSqlRewriteSettings(SqlRewriteSettings settings);
    }
}
