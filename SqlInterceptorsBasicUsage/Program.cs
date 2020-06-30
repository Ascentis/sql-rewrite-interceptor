using System;
using System.Data.SqlClient;
using Ascentis.Infrastructure.DBRepository;
using Ascentis.Infrastructure.SqlInterceptors;
using Ascentis.Infrastructure.SqlInterceptors.Model;
using Ascentis.Infrastructure.SqlInterceptors.Repository;
using SqlInterceptorsBasicUsage.Properties;

namespace SqlInterceptorsBasicUsage
{
    internal static class Program
    {
        private static void Main()
        {
            Console.TreatControlCAsInput = true;

            using var repo = new SqlRewriteDbRepository(Settings.Default.ConnectionString);
            repo.RemoveAllSqlRewriteRules();
            var lastRefreshFromRepo = DateTime.Now;

            using var svc = new SqlRewriteRuleService(repo, true) { AutoRefreshRulesAndSettingsEnabled = true };
            svc.AutoRefreshEvent += () => lastRefreshFromRepo = DateTime.Now;

            using var conn = new SqlConnection(Settings.Default.ConnectionString);
            conn.Open();

            SqlRewriteRule rule = null;
            var input = "";
            do
            {
                Console.Clear();
                if (input != "")
                {
                    rule ??= new SqlRewriteRule()
                    {
                        DatabaseRegEx = ".*",
                        QueryMatchRegEx = "SELECT @@VERSION"
                    };
                    rule.QueryReplacementString = $"{input} /*x*/";
                    repo.SaveSqlRewriteRule(rule);
                }

                PrintCurrentRuleSet(repo);

                using var cmd = CreateSqlCommand(conn);
                Console.WriteLine($"Current datetime: {DateTime.Now}");
                Console.WriteLine($"Last refresh from repository: {lastRefreshFromRepo}");
                Console.WriteLine($"Auto-refresh interval (milliseconds): {svc.AutoRefreshTimerInterval}");
                Console.WriteLine($"Query:\r\n{cmd.CommandText}\r\n");
                Console.WriteLine($"Result:\r\n{cmd.ExecuteScalar()}\r\n");
                Console.WriteLine("Press Enter to continue, enter a replacement SQL and press Enter or press Ctrl-C to finish execution");
                input = "";
                do
                {
                    var keyPressed = Console.ReadKey();
                    if (keyPressed.Key == ConsoleKey.C && keyPressed.Modifiers == ConsoleModifiers.Control)
                        return;
                    if (keyPressed.Key == ConsoleKey.Enter)
                        break;
                    input += keyPressed.KeyChar;
                } while (true);
                Console.WriteLine();
            } while (true);
        }

        private static void PrintCurrentRuleSet(ISqlRewriteRepository repo)
        {
            var currentRules = repo.LoadSqlRewriteRules();
            Console.WriteLine("Current rule set in database:\r\n");
            foreach (var loadedRule in currentRules)
            {
                Console.WriteLine($"RuleID                  : {loadedRule.Id}");
                Console.WriteLine($"Database RegEx          : {loadedRule.DatabaseRegEx}");
                Console.WriteLine($"Query match RegEx       : {loadedRule.QueryMatchRegEx}");
                Console.WriteLine($"Query replacement RegEx : {loadedRule.QueryReplacementString}");
                Console.WriteLine($"RegExOptions RegEx      : {loadedRule.RegExOptions}\r\n");
            }
        }

        private static SqlCommand CreateSqlCommand(SqlConnection conn)
        {
            return new SqlCommand("SELECT @@VERSION", conn);
        }
    }
}
