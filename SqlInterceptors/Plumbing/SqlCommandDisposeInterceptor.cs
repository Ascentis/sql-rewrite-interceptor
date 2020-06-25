using System.Data.SqlClient;
using HarmonyLib;
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace Ascentis.Infrastructure
{
    [HarmonyPatch(typeof(SqlCommand))]
    [HarmonyPatch("Dispose")]
    public class SqlCommandDisposeInterceptor
    {
        private static void Prefix(SqlCommand __instance)
        {
            if (SqlCommandTextStackTraceInjector.HashInjectionEnabled || SqlCommandTextStackTraceInjector.StackInjectionEnabled)
                SqlCommandTextStackTraceInjector.RemoveSqlCommandFromDictionary(__instance);
        }
    }
}
