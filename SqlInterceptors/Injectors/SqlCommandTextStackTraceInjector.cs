using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
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
        private static readonly ConcurrentObjectAccessor<List<string>> StackFrameIgnorePrefixesList = new ConcurrentObjectAccessor<List<string>>();
        public static string StackFrameIgnorePrefixes
        {
            get => StackFrameIgnorePrefixesList.ExecuteReadLocked( prefixList => string.Join("\r\n", prefixList));
            set
            {
                StackFrameIgnorePrefixesList.SwapNewAndExecute(
                    newPrefixList =>
                    {
                        var entries = value.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
                        newPrefixList.AddRange(entries.Select(entry => entry.Trim()));
                    }, oldPrefixList => { });
            }
        }

        public static bool MatchStackFrameEntry(string entry)
        {
            return StackFrameIgnorePrefixesList.ExecuteReadLocked( prefixList => prefixList.Any(entry.StartsWith));
        }

        public static string InjectStackTrace(DbConnection dbConnection, SqlCommand sqlCmd, string sqlCommand, CommandType commandType)
        {
            try
            {
                if (SqlCommandInterceptor.Enabled && (HashInjectionEnabled || StackInjectionEnabled))
                    StoreSqlCommandInDictionary(sqlCmd, sqlCommand);
                if (!SqlCommandInterceptor.Enabled || commandType != CommandType.Text || sqlCommand.StartsWith(WasProcessedIndicator))
                    return sqlCommand;
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
                            var stackEntry = $"[{memberInfo.Assembly.GetName().Name}].{memberInfo.FullName}.{stackFrame.GetMethod().Name}";
                            if (MatchStackFrameEntry(stackEntry))
                                continue;
                            callStack += $"{stackEntry}\r\n";
                            if(stackEntries-- <= 0)
                                break;
                        }
                    }

                    if (callStack != "")
                        stackTraceText = $"\r\n/* Top {CallStackEntriesToReport} call stack entries:\r\n{callStack} */";
                }

                var hashText = "";
                // ReSharper disable once InvertIf
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