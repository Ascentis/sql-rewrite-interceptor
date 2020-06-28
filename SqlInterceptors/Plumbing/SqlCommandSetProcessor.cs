﻿using System;
using System.Data.SqlClient;
using Ascentis.Infrastructure.SqlInterceptors.Injectors;
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InconsistentNaming

namespace Ascentis.Infrastructure.SqlInterceptors.Plumbing
{
    public class SqlCommandSetProcessor
    {
        public static string Process(SqlCommand __instance, string cmdText, SqlConnection connection)
        {
            var replacedCmdText = cmdText;
            try
            {
                if (SqlCommandInterceptor.SqlCommandProcessorEvent == null)
                    return replacedCmdText;
                foreach (var chainedSqlCommandDelegate in SqlCommandInterceptor.SqlCommandProcessorEvent.GetInvocationList())
                    replacedCmdText = ((SqlCommandInterceptor.SqlCommandProcessorDelegate)chainedSqlCommandDelegate)(connection, __instance, replacedCmdText, __instance.CommandType);
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
