﻿using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using Ascentis.Infrastructure.Properties;

namespace Ascentis.Infrastructure
{
    public class SqlCommandTextStackTraceInjector
    {
        private static ConcurrentDictionary<SqlCommand, string> _originalSqlCommand = new ConcurrentDictionary<SqlCommand, string>();
        public static bool HashInjectionEnabled = Settings.Default.HashInjectionEnabled;
        public static bool StackInjectionEnabled = Settings.Default.StackFrameInjectionEnabled;
        public static int CallStackEntriesToReport = Settings.Default.StackEntriesReportedCount;
        public static string InjectStackTrace(DbConnection dbConnection, string sqlCommand, CommandType commandType)
        {
            if (!SqlCommandProcessor.Enabled || commandType != CommandType.Text || sqlCommand.StartsWith("/*-*/"))
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

                return $"/*-*/{hashText}{sqlCommand}{stackTraceText}";
            }
            catch (Exception e)
            {
                SqlCommandInterceptor.ExceptionDelegateEvent?.Invoke(e);
                HashInjectionEnabled = false;
                return sqlCommand;
            }
        }

        public static void AddSqlCommandToDictionary(SqlCommand cmd, string cmdText)
        {
            _originalSqlCommand.TryAdd(cmd, cmdText);
        }

        public static void RemoveSqlCommandFromDictionary(SqlCommand cmd)
        {
            // ReSharper disable once UnusedVariable
            _originalSqlCommand.TryRemove(cmd, out var cmdText);
        }

        public static bool GetOriginalSqlCommandFromDictionary(SqlCommand cmd, out string sql)
        {
            return _originalSqlCommand.TryGetValue(cmd, out sql);
        }
    }
}