using System.Collections.Generic;

namespace Ascentis.Infrastructure
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
