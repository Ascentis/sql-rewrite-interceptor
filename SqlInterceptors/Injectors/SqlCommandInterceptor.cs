using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Ascentis.Infrastructure.SqlInterceptors.Injectors
{
    public static class SqlCommandInterceptor
    {
        public static bool Enabled { get; set; }
        public delegate string SqlCommandProcessorDelegate(DbConnection dbConnection, SqlCommand sqlCmd, string value, CommandType commandType);
        public delegate void ExceptionDelegate(Exception e);
        public static SqlCommandProcessorDelegate SqlCommandProcessorEvent { get; set; }
        public static event ExceptionDelegate ExceptionDelegateEvent;

        public static void OnExceptionDelegateEvent(Exception e)
        {
            ExceptionDelegateEvent?.Invoke(e);
        }
    }
}