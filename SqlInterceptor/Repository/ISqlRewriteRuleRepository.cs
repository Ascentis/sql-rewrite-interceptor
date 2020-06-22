using System.Collections.Generic;

namespace Ascentis.Infrastructure
{
    public interface ISqlRewriteRuleRepository
    {
        void Save(SqlRewriteRule rule);
        void Remove(int id);
        IEnumerable<SqlRewriteRule> Load();
        bool IsThreadSafe();
    }
}
