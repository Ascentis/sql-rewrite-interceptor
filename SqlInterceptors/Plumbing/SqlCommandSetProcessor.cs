using System.Data.SqlClient;
using Ascentis.Infrastructure.SqlInterceptors.Injectors;
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InconsistentNaming

namespace Ascentis.Infrastructure.SqlInterceptors.Plumbing
{
    public static class SqlCommandSetProcessor
    {
        public static string Process(SqlConnection connection, SqlCommand __instance, string cmdText)
        {
            return SqlCommandInterceptor.OnSqlCommandProcessorEvent(connection, __instance, cmdText);
        }
    }
}
