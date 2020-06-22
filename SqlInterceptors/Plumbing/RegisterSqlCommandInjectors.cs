namespace Ascentis.Infrastructure
{
    public class RegisterSqlCommandInjectors
    {
        public static void Register()
        {
            SqlCommandInterceptor.SqlCommandSetEvent += SqlCommandTextStackTraceInjector.InjectStackTrace;
            SqlCommandInterceptor.SqlCommandSetEvent += SqlCommandRegExProcessor.ProcessSqlForRegExReplacement;
        }}
}
