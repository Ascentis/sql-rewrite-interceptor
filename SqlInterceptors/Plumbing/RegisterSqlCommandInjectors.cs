using Ascentis.Infrastructure.SqlInterceptors.Injectors;

namespace Ascentis.Infrastructure.SqlInterceptors.Plumbing
{
    public static class RegisterSqlCommandInjectors
    {
        public static void Register()
        {
            SqlCommandInterceptor.SqlCommandProcessorEvent += SqlCommandTextStackTraceInjector.InjectStackTrace;
            SqlCommandInterceptor.SqlCommandProcessorEvent += SqlCommandRegExProcessor.ProcessSqlForRegExReplacement;
        }}
}
