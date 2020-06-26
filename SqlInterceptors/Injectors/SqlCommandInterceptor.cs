using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Ascentis.Infrastructure.SqlInterceptors.Injectors
{
    public class SqlCommandInterceptor
    {
        public delegate string SqlCommandProcessorDelegate(DbConnection dbConnection, SqlCommand sqlCmd, string value, CommandType commandType);
        public delegate void ExceptionDelegate(Exception e);
        public static SqlCommandProcessorDelegate SqlCommandProcessorEvent;
        public static ExceptionDelegate ExceptionDelegateEvent;
    }
}