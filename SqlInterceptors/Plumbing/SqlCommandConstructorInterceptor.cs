using System.Data.SqlClient;
using HarmonyLib;
// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

namespace Ascentis.Infrastructure.SqlInterceptors.Plumbing
{
    [HarmonyPatch(typeof(SqlCommand), MethodType.Constructor)]
    [HarmonyPatch(new[] { typeof(string), typeof(SqlConnection) })]
    public class SqlCommandCommandConstructorInterceptor_1
    {
        static void Postfix(SqlCommand __instance, string cmdText, SqlConnection connection)
        {
            __instance.CommandText = SqlCommandSetProcessor.Process(__instance, cmdText, connection);
        }
    }

    [HarmonyPatch(typeof(SqlCommand), MethodType.Constructor)]
    [HarmonyPatch(new[] { typeof(string), typeof(SqlConnection), typeof(SqlTransaction) })]
    public class SqlCommandCommandConstructorInterceptor_2
    {
        static void Postfix(SqlCommand __instance, string cmdText, SqlConnection connection, SqlTransaction transaction)
        {
            __instance.CommandText = SqlCommandSetProcessor.Process(__instance, cmdText, connection);
        }
    }

    [HarmonyPatch(typeof(SqlCommand), MethodType.Constructor)]
    [HarmonyPatch(new[] { typeof(string), typeof(SqlConnection), typeof(SqlTransaction), typeof(SqlCommandColumnEncryptionSetting) })]
    public class SqlCommandCommandConstructorInterceptor_3
    {
        static void Postfix(SqlCommand __instance, string cmdText, SqlConnection connection, SqlTransaction transaction, SqlCommandColumnEncryptionSetting columnEncryptionSetting)
        {
            __instance.CommandText = SqlCommandSetProcessor.Process(__instance, cmdText, connection);
        }
    }

    [HarmonyPatch(typeof(SqlCommand), MethodType.Constructor)]
    [HarmonyPatch(new[] { typeof(SqlCommand) })]
    public class SqlCommandCommandConstructorInterceptor_4
    {
        static void Postfix(SqlCommand __instance, SqlCommand from)
        {
            __instance.CommandText = SqlCommandSetProcessor.Process(__instance, from.CommandText, from.Connection);
        }
    }
}