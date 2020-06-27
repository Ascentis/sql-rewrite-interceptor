using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using Ascentis.Infrastructure.SqlInterceptors.Properties;

namespace Ascentis.Infrastructure.SqlInterceptors.Injectors
{
    public class SqlCommandTextStackTraceInjector
    {
        private const string WasProcessedIndicator = "/*-*/";
        private static readonly ConcurrentDictionary<SqlCommand, string> OriginalSqlCommand = new ConcurrentDictionary<SqlCommand, string>();
        public static bool HashInjectionEnabled = Settings.Default.HashInjectionEnabled;
        public static bool StackInjectionEnabled = Settings.Default.StackFrameInjectionEnabled;
        public static int CallStackEntriesToReport = Settings.Default.StackEntriesReportedCount;

        public static string InjectStackTrace(DbConnection dbConnection, SqlCommand sqlCmd, string sqlCommand, CommandType commandType)
        {
            if (SqlCommandInterceptor.Enabled && (HashInjectionEnabled || StackInjectionEnabled))
                StoreSqlCommandInDictionary(sqlCmd, sqlCommand);
            if (!SqlCommandInterceptor.Enabled || commandType != CommandType.Text || sqlCommand.StartsWith(WasProcessedIndicator))
                return sqlCommand;
            try
            {
                var stackTraceText = "";
                if (StackInjectionEnabled)
                {
                    var callStack = "";
                    var stackTrace = new StackTrace();
                    var stackFrames = stackTrace.GetFrames();
                    if (stackFrames != null)
                    {
                        var stackEntries = CallStackEntriesToReport;
                        foreach (var stackFrame in stackFrames)
                        {
                            var memberInfo = stackFrame.GetMethod().DeclaringType;
                            if (memberInfo == null || memberInfo.Assembly == Assembly.GetExecutingAssembly() || memberInfo.Assembly.FullName.Contains("mscorlib"))
                                continue;
                            callStack += $"{memberInfo.FullName}.{stackFrame.GetMethod().Name}\r\n";
                            if(stackEntries-- <= 0)
                                break;
                        }
                    }

                    if (callStack != "")
                        stackTraceText = $"\r\n/* Top {CallStackEntriesToReport} call stack entries:\r\n{callStack} */";
                }

                var hashText = "";
                if (HashInjectionEnabled)
                {
                    var hash = (uint) sqlCommand.GetHashCode();
                    hashText = $"/*AHSH={hash}*/ ";
                }

                return $"{WasProcessedIndicator}{hashText}{sqlCommand}{stackTraceText}";
            }
            catch (Exception e)
            {
                SqlCommandInterceptor.ExceptionDelegateEvent?.Invoke(e);
                HashInjectionEnabled = false;
                return sqlCommand;
            }
        }

        public static void StoreSqlCommandInDictionary(SqlCommand cmd, string cmdText)
        {
            if (!cmdText.StartsWith(WasProcessedIndicator))
                OriginalSqlCommand.AddOrUpdate(cmd, (v) => cmdText, (k, v) => cmdText);
        }

        public static void RemoveSqlCommandFromDictionary(SqlCommand cmd)
        {
            // ReSharper disable once UnusedVariable
            OriginalSqlCommand.TryRemove(cmd, out var cmdText);
        }

        public static bool TryGetOriginalSqlCommandFromDictionary(SqlCommand cmd, out string sql)
        {
            return OriginalSqlCommand.TryGetValue(cmd, out sql);
        }
    }
}