using System;

namespace Ascentis.Infrastructure.DBRepository
{
    public class SqlRewriteRuleDbRepositoryException : Exception
    {
        public SqlRewriteRuleDbRepositoryException(string msg) : base(msg) {}
    }
}
