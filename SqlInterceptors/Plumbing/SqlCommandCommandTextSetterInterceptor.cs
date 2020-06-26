using System.Data.SqlClient;
using HarmonyLib;
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace Ascentis.Infrastructure.SqlInterceptors.Plumbing
{
    [HarmonyPatch(typeof(SqlCommand))]
    [HarmonyPatch("CommandText", MethodType.Setter)]
    public class SqlCommandCommandTextSetterInterceptor
    {
        private static void Prefix(SqlCommand __instance, ref string value)
        {
            value = SqlCommandSetProcessor.Process(__instance, value, __instance.Connection);
        }
    }
}
