#Sample usage

```C#

using System;
using System.Data.SqlClient;
using Ascentis.Infrastructure;

namespace ConsoleApp2
{
    class Program
    {
        private const string Cs = "Server=vm-pc-sql02; Database=master; Trusted_Connection=True;";

        static void Main()
        {
            using (var repo = new SqlRewriteDbRepository(Cs))
            using (var svc = new SqlRewriteRuleService(repo))
            {
                var id = svc.AddRule(".*", "(.+ +)@@VERSION", "$1GETDATE()");
                try
                {
                    svc.Enabled = true;
                    using (var conn = new SqlConnection(Cs))
                    using (var cmd = new SqlCommand("SELECT   @@VERSION", conn))
                    {
                        conn.Open();
                        Console.WriteLine($"SQL: {cmd.CommandText}\r\nResult: {cmd.ExecuteScalar()}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception: {e.Message}");
                }
                finally
                {
                    svc.RemoveRule(id);
                    Console.ReadLine();
                }
            }
        }
    }
}
```