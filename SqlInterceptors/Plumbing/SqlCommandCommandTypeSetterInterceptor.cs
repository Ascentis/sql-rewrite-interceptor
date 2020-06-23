using System.Data;
using System.Data.SqlClient;
using HarmonyLib;

namespace Ascentis.Infrastructure
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
