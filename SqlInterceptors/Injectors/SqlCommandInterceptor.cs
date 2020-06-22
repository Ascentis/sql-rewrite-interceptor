using System;
using System.Data.Common;

namespace Ascentis.Infrastructure
{
    public class SqlCommandInterceptor
    {
        public delegate string SqlCommandSetterDelegate(DbConnection dbConnection, string value);
        public delegate void ExceptionDelegate(Exception e);
        public static SqlCommandSetterDelegate SqlCommandSetEvent;
        public static ExceptionDelegate ExceptionDelegateEvent;
    }
}