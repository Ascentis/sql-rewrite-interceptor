using System;

namespace Ascentis.Infrastructure.SqlInterceptors
{
    public class SqlRewriteRuleServiceException : Exception
    {
        public SqlRewriteRuleServiceException(string msg) : base(msg) {}
    }
}
