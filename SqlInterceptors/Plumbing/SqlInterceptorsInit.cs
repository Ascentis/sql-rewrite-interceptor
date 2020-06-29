using HarmonyLib;

namespace Ascentis.Infrastructure.SqlInterceptors.Plumbing
{
    public static class SqlInterceptorsInit
    {
        private static Harmony _harmony;

        public static void Init()
        {
            if (_harmony != null)
                return;
            _harmony = new Harmony("SqlInterceptors");
            _harmony.PatchAll();
        }
    }
}
