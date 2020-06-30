using System;
using System.Runtime.Caching;
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
    public static class SqlCommandTextStackTraceInjector
    {
        private const string WasProcessedIndicator = "/*-*/";
        private static readonly SimpleMemoryCache OriginalSqlCommand = new SimpleMemoryCache($"{nameof(SqlCommandTextStackTraceInjector)}_OriginalSqlCache");
        public static bool HashInjectionEnabled = Settings.Default.HashInjectionEnabled;
        public static bool StackInjectionEnabled = Settings.Default.StackFrameInjectionEnabled;
        public static int CallStackEntriesToReport = Settings.Default.StackEntriesReportedCount;
        private static readonly ConcurrentObjectAccessor<List<string>> StackFrameIgnorePrefixesList = new ConcurrentObjectAccessor<List<string>>();

        public static string StackFrameIgnorePrefixes
        {
            get => StackFrameIgnorePrefixesList.ExecuteReadLocked(prefixList => string.Join("\r\n", prefixList));
            set
            {
                StackFrameIgnorePrefixesList.SwapNewAndExecute(
                    newPrefixList =>
                    {
                        var entries = value.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
                        newPrefixList.AddRange(entries.Select(entry => entry.Trim()));
                    });
            }
        }

        static SqlCommandTextStackTraceInjector()
        {
            StackFrameIgnorePrefixes = Settings.Default.StackFrameIgnorePrefixes;
        }

        public static bool MatchStackFrameEntry(string entry)
        {
            return StackFrameIgnorePrefixesList.ExecuteReadLocked( prefixList => prefixList.Any(entry.StartsWith));
        }

        private static string GetStackTrace()
        {
            if (!StackInjectionEnabled) 
                return "";
            var stackTrace = new StackTrace();
            var stackFrames = stackTrace.GetFrames();
            if (stackFrames == null)
                return "";
            var callStack = "";
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
                if (stackEntries-- <= 0)
                    break;
            }

            return callStack == "" ? "" : $"\r\n/* Top {CallStackEntriesToReport} call stack entries:\r\n{callStack} */";
        }

        private static string GetSqlCommandHash(string sqlCommand)
        {
            if (!HashInjectionEnabled) 
                return "";
            var hash = (uint) sqlCommand.GetHashCode();
            return $"/*AHSH={hash}*/ ";
        }

        public static string InjectStackTrace(DbConnection dbConnection, SqlCommand sqlCmd, string sqlCommand, CommandType commandType)
        {
            try
            {
                if (SqlCommandInterceptor.Enabled && (HashInjectionEnabled || StackInjectionEnabled))
                    StoreSqlCommandInDictionary(sqlCmd, sqlCommand);
                if (!SqlCommandInterceptor.Enabled || commandType != CommandType.Text || sqlCommand.StartsWith(WasProcessedIndicator))
                    return sqlCommand;
                var stackTraceText = GetStackTrace();
                var hashText = GetSqlCommandHash(sqlCommand);
                return $"{WasProcessedIndicator}{hashText}{sqlCommand}{stackTraceText}";
            }
            catch (Exception e)
            {
                SqlCommandInterceptor.OnExceptionDelegateEvent(e);
                return sqlCommand;
            }
        }

        public static void StoreSqlCommandInDictionary(SqlCommand cmd, string cmdText)
        {
            if (!cmdText.StartsWith(WasProcessedIndicator))
                OriginalSqlCommand.Set(cmd.GetHashCode().ToString(), v => cmdText, new CacheItemPolicy() { SlidingExpiration = new TimeSpan(0, 0, 1, 0) });
        }

        public static void RemoveSqlCommandFromDictionary(SqlCommand cmd)
        {
            OriginalSqlCommand.Remove(cmd.GetHashCode().ToString());
        }

        public static bool TryGetOriginalSqlCommandFromDictionary(SqlCommand cmd, out string sql)
        {
            var found = OriginalSqlCommand.Get(cmd.GetHashCode().ToString(), out var oSql);
            sql = oSql != null ? (string) oSql : "";
            return found;
        }
    }
}