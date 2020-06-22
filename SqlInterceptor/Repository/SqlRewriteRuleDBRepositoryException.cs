using System;

namespace Ascentis.Infrastructure
{
    public class SqlRewriteRuleDbRepositoryException : Exception
    {
        public SqlRewriteRuleDbRepositoryException(string msg) : base(msg) {}
    }
}
