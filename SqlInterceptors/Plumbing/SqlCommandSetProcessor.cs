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
                if (SqlCommandTextStackTraceInjector.HashInjectionEnabled || SqlCommandTextStackTraceInjector.StackInjectionEnabled)
                    SqlCommandTextStackTraceInjector.AddSqlCommandToDictionary(__instance, cmdText);
                foreach (var chainedSqlCommandDelegate in SqlCommandInterceptor.SqlCommandSetEvent.GetInvocationList())
                {
                    replacedCmdText = (string) chainedSqlCommandDelegate.DynamicInvoke(__instance.Connection, replacedCmdText, __instance.CommandType);
                }

                return replacedCmdText;
            }
            catch (Exception e)
            {
                SqlCommandInterceptor.ExceptionDelegateEvent?.Invoke(e);
                return cmdText;
            }
        }
    }
}
