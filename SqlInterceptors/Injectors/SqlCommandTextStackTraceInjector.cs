using System;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using Ascentis.Infrastructure.Properties;

namespace Ascentis.Infrastructure
{
    public class SqlCommandTextStackTraceInjector
    {
        public static bool HashInjectionEnabled = Settings.Default.HashInjectionEnabled;
        public static bool StackInjectionEnabled = Settings.Default.StackFrameInjectionEnabled;
        public static string InjectStackTrace(DbConnection dbConnection, string sqlCommand)
        {
            if (!SqlCommandProcessor.Enabled || sqlCommand.Contains("/*AHSH=") || sqlCommand.Contains("/*MTDNM="))
                return sqlCommand;
            try
            {
                var instrumentedSqlCmd = sqlCommand;
                if (StackInjectionEnabled)
                {
                    var callerMethodName = "";
                    var stackTrace = new StackTrace();
                    var stackFrames = stackTrace.GetFrames();
                    if (stackFrames != null)
                        foreach (var stackFrame in stackFrames)
                        {
                            var memberInfo = stackFrame.GetMethod().DeclaringType;
                            if (memberInfo == null || memberInfo.Assembly == Assembly.GetExecutingAssembly() || memberInfo.Assembly.FullName.Contains("mscorlib"))
                                continue;
                            callerMethodName = memberInfo.FullName + '.' + stackFrame.GetMethod().Name;
                            break;
                        }

                    if (callerMethodName != "")
                        instrumentedSqlCmd = $"/*MTDNM={callerMethodName}*/{instrumentedSqlCmd}";
                }

                if (!HashInjectionEnabled) 
                    return instrumentedSqlCmd;

                var hash = (uint) sqlCommand.GetHashCode();
                return $"/*AHSH={hash}*/{instrumentedSqlCmd}";
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