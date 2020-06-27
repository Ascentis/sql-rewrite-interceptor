#define DISABLE_PATCH_DISPOSE

using System.Data.SqlClient;
using Ascentis.Infrastructure.SqlInterceptors.Injectors;
// ReSharper disable once RedundantUsingDirective
using HarmonyLib;
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace Ascentis.Infrastructure.SqlInterceptors.Plumbing
{
#if !_DISABLE_PATCH_DISPOSE
    /* For some reason patching of SqlCommand.Dispose() fails when trying to run while debugging */
    [HarmonyPatch(typeof(SqlCommand))]
    [HarmonyPatch("Dispose")]
#endif
    public class SqlCommandDisposeInterceptor
    {
        private static void Prefix(SqlCommand __instance)
        {
            if (SqlCommandTextStackTraceInjector.HashInjectionEnabled || SqlCommandTextStackTraceInjector.StackInjectionEnabled)
                SqlCommandTextStackTraceInjector.RemoveSqlCommandFromDictionary(__instance);
        }
    }
}
