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

        public static event SqlCommandProcessorDelegate SqlCommandProcessorEvent;
        public static event ExceptionDelegate ExceptionDelegateEvent;

        public static void OnExceptionDelegateEvent(Exception e)
        {
            ExceptionDelegateEvent?.Invoke(e);
        }

        public static string OnSqlCommandProcessorEvent(SqlConnection connection, SqlCommand sqlCommand, string cmdText)
        {
            if (SqlCommandProcessorEvent == null)
                return cmdText;
            try
            {
                var replacedCmdText = cmdText;
                foreach (var chainedSqlCommandDelegate in SqlCommandProcessorEvent.GetInvocationList())
                    replacedCmdText = ((SqlCommandProcessorDelegate) chainedSqlCommandDelegate)(connection, sqlCommand, replacedCmdText, sqlCommand.CommandType);
                return replacedCmdText;
            }
            catch (Exception e)
            {
                OnExceptionDelegateEvent(e);
                return cmdText;
            }
        }
    }
}