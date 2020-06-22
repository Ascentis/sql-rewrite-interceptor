using System;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using SqlInterceptor.Properties;

namespace Ascentis.Infrastructure
{
    public class SqlCommandTextStackTraceInjector : SqlCommandProcessorBase
    {
        public static bool HashInjectionEnabled = Settings.Default.HashInjectionEnabled;
        public static string InjectStackTrace(DbConnection dbConnection, string sqlCommand)
        {
            if (!HashInjectionEnabled || !Enabled || sqlCommand.StartsWith("/* AHSH="))
                return sqlCommand;
            try
            {
                var hash = (uint) sqlCommand.GetHashCode();

                var stackTrace = new StackTrace();
                var stackFrames = stackTrace.GetFrames();

                var callerMethodName = "";
                if (stackFrames == null)
                    return $"/* AHSH={hash} */ {sqlCommand}";
                foreach (var stackFrame in stackFrames)
                {
                    var memberInfo = stackFrame.GetMethod().DeclaringType;
                    if (memberInfo == null || memberInfo.Assembly == Assembly.GetExecutingAssembly() ||
                        memberInfo.Assembly.FullName.Contains("mscorlib"))
                        continue;
                    callerMethodName = memberInfo.FullName + '.' + stackFrame.GetMethod().Name;
                    break;
                }

                return $"/* AHSH={hash} MTDNM={callerMethodName} */ {sqlCommand}";
            }
            catch (Exception e)
            {
                SqlCommandInterceptor.ExceptionDelegateEvent?.Invoke(e);
                HashInjectionEnabled = false;
                return sqlCommand;
            }
        }
    }
}