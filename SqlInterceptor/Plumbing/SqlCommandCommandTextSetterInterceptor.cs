using System;
using System.Data.Common;
using System.Data.SqlClient;
using HarmonyLib;

namespace Ascentis.Infrastructure
{
    public class SqlCommandInterceptor
    {
        public delegate string SqlCommandSetterDelegate(DbConnection dbConnection, string value);
        public static SqlCommandSetterDelegate SqlCommandSetEvent;
    }

    [HarmonyPatch(typeof(SqlCommand))]
    [HarmonyPatch("CommandText", MethodType.Setter)]
    public class SqlCommandCommandTextSetterInterceptor
    {
        // ReSharper disable once InconsistentNaming
        private static void Prefix(SqlCommand __instance, ref string value)
        {
            value = SqlCommandSetProcessor.Process(__instance, value, __instance.Connection);
        }
    }
}
