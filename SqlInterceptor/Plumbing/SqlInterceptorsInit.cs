using HarmonyLib;

namespace Ascentis.Infrastructure
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
