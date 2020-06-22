using System;
using System.Data.SqlClient;
// ReSharper disable InconsistentNaming

namespace Ascentis.Infrastructure
{
    public class SqlCommandSetProcessor
    {
        public static string Process(SqlCommand __instance, string cmdText, SqlConnection connection)
        {
            var replacedCmdText = cmdText;
            try
            {
                if (SqlCommandInterceptor.SqlCommandSetEvent == null)
                    return replacedCmdText;
                foreach (var chainedSqlCommandDelegate in SqlCommandInterceptor.SqlCommandSetEvent.GetInvocationList())
                {
                    replacedCmdText = (string) chainedSqlCommandDelegate.DynamicInvoke(__instance.Connection, replacedCmdText);
                }

                return replacedCmdText;
            }
            catch (Exception)
            {
                // Ignore any exception. We don't want to prevent app from operating because our interceptor errored out
                return cmdText;
            }
        }
    }
}
