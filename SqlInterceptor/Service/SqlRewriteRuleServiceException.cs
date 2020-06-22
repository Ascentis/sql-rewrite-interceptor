using System;

namespace Ascentis.Infrastructure
{
    public class SqlRewriteRuleServiceException : Exception
    {
        public SqlRewriteRuleServiceException(string msg) : base(msg) {}
    }
}
