using System.Data.Common;
using System.Diagnostics;
using System.Reflection;

namespace Ascentis.Infrastructure
{
    public class SqlCommandTextStackTraceInjector : SqlCommandProcessorBase
    {
        public static string InjectStackTrace(DbConnection dbConnection, string sqlCommand)
        {
            if (!Enabled || sqlCommand.StartsWith("/* AHSH="))
                return sqlCommand;
            var hash = (uint)sqlCommand.GetHashCode();

            var stackTrace = new StackTrace();
            var stackFrames = stackTrace.GetFrames();

            var callerMethodName = "";
            if (stackFrames == null)
                return $"/* AHSH={hash} */ {sqlCommand}";
            foreach (var stackFrame in stackFrames)
            {
                var memberInfo = stackFrame.GetMethod().DeclaringType;
                if (memberInfo == null || memberInfo.Assembly == Assembly.GetExecutingAssembly() || memberInfo.Assembly.FullName.Contains("mscorlib"))
                    continue;
                callerMethodName = memberInfo.FullName + '.' + stackFrame.GetMethod().Name;
                break;
            }

            return $"/* AHSH={hash} MTDNM={callerMethodName} */ {sqlCommand}";
        }
    }
}
