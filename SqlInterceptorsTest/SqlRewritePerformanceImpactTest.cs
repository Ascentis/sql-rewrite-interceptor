using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using Ascentis.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlInterceptorsTest.Properties;

namespace SqlInterceptorsTest
{
    [TestClass]
    public class SqlRewritePerformanceImpactTest
    {
        private const string Stm = "SELECT @@VERSION";

        [TestInitialize]
        public void TestInitialize()
        {
            using (var conn = new SqlConnection(Settings.Default.ConnectionString))
            {
                conn.Open();
                using (var truncateTable = new SqlCommand("TRUNCATE TABLE SqlRewriteRegistry", conn))
                {
                    try
                    {
                        truncateTable.ExecuteNonQuery();
                    }
                    catch (SqlException)
                    {
                        // Ignore exceptions. Table may not exist
                    }
                }
            }
        }

        [TestMethod]
        public void TestPerformanceImpact()
        {
            using (var repo = new SqlRewriteDbRepository(Settings.Default.ConnectionString))
            {
                using (var service = new SqlRewriteRuleService(repo, true))
                {
                    using (var conn = new SqlConnection(Settings.Default.ConnectionString))
                    {
                        //SqlCommandTextStackTraceInjector.HashInjectionEnabled = false;
                        //SqlCommandTextStackTraceInjector.StackInjectionEnabled = false;
                        conn.Open();
                        var stopWatch = new Stopwatch();
                        stopWatch.Start();
                        Assert.AreNotEqual(0, service.AddRule(".*", Stm, "SELECT GETDATE()"));
                        service.RefreshRulesFromRepository();
                        for (var i = 0; i < 10000; i++)
                        {
                            using (var cmd = new SqlCommand(Stm, conn))
                            {
                                var version = cmd.ExecuteScalar().ToString();
                                Assert.IsNotNull(version.Contains("Microsoft"));
                            }
                        }
                        stopWatch.Stop();
                        var intervalFullInjection = stopWatch.Elapsed;
                        stopWatch.Reset();
                        stopWatch.Start();
                        service.Enabled = false;
                        for (var i = 0; i < 10000; i++)
                        {
                            using (var cmd = new SqlCommand(Stm, conn))
                            {
                                var version = cmd.ExecuteScalar().ToString();
                                Assert.IsNotNull(version.Contains("Microsoft"));
                            }
                        }
                        Assert.AreEqual(intervalFullInjection, stopWatch.Elapsed);
                    }
                }
            }
        }
    }
}
