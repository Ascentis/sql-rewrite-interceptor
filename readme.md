# SQL-Rewrite-Interceptor

## Description

This package provides the ability to instrument a .NET application reliant on ado.net in a way that allows SQL re-write (sql text modification) in
a production environment without requiring applicaiton re-compilation.

This allows for query fast deployment (hotfixing) of query optimization while working on changing source code application logic.

## Nuget package name

Ascentis.SQLRewriteInterceptor

## Dependencies

- Lib.Harmony
- Ascentis.Infrastructure

## Sample usage

```C#

using System;
using System.Data.SqlClient;
using Ascentis.Infrastructure;

namespace ConsoleApp
{
    class Program
    {
        private const string Cs = "Server=<Your Server Name>; Database=<Your Database>; Trusted_Connection=True;";

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