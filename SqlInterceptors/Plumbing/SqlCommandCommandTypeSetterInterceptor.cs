using System.Data;
using System.Data.SqlClient;
using Ascentis.Infrastructure.SqlInterceptors.Injectors;
using HarmonyLib;
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace Ascentis.Infrastructure.SqlInterceptors.Plumbing
{
    [HarmonyPatch(typeof(SqlCommand))]
    [HarmonyPatch("CommandType", MethodType.Setter)]
    public class SqlCommandCommandTypeSetterInterceptor
    {
        private static void Postfix(SqlCommand __instance, CommandType value)
        {
            if (value != CommandType.Text && SqlCommandTextStackTraceInjector.GetOriginalSqlCommandFromDictionary(__instance, out var originalSql))
                __instance.CommandText = originalSql;
        }
    }
}
