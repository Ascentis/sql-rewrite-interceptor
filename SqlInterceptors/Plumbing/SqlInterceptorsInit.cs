using HarmonyLib;

namespace Ascentis.Infrastructure.SqlInterceptors.Plumbing
{
    public class SqlInterceptorsInit
    {
        public static void Init()
        {
            var harmony = new Harmony("SqlInterceptors");
            harmony.PatchAll();
        }
    }
}
