using System;
using System.Data.SqlClient;
using System.Threading;
using Ascentis.Infrastructure.DBRepository;
using Ascentis.Infrastructure.SqlInterceptors;
using Ascentis.Infrastructure.SqlInterceptors.Injectors;
using Ascentis.Infrastructure.SqlInterceptors.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlInterceptorsTest.Properties;

namespace SqlInterceptorsTest
{
    [TestClass]
    public class SqlRewriteRuleServiceTests
    {
        private const string Stm = "SELECT @@VERSION";

        [TestInitialize]
        public void TestInitialize()
        {
            TestUtils.InitTests();
        }

        [TestMethod]
        public void TestCreateSqlRewriteRuleService()
        {
            using var conn = new SqlConnection(Settings.Default.ConnectionString);
            using var service = new SqlRewriteRuleService(new SqlRewriteDbRepository(conn));
            Assert.IsNotNull(service);
        }

        [TestMethod]
        public void TestBasicSqlRewriteRuleService()
        {
            using var conn = new SqlConnection(Settings.Default.ConnectionString);
            var repo = new SqlRewriteDbRepository(conn);
            var rule = new SqlRewriteRule
            {
                DatabaseRegEx = ".*", QueryMatchRegEx = Stm, QueryReplacementString = "SELECT GETDATE()"
            };
            repo.SaveSqlRewriteRule(rule);
            using var service = new SqlRewriteRuleService(repo, true);
            using var con = new SqlConnection(Settings.Default.ConnectionString);
            con.Open();
            using var cmd = new SqlCommand(Stm, con);
            var version = cmd.ExecuteScalar().ToString();
            Assert.IsFalse(version.Contains("Microsoft"));
            service.RemoveRule(rule.Id);
            service.RefreshRulesFromRepository();
            using var con2 = new SqlConnection(Settings.Default.ConnectionString);
            con2.Open();
            using var cmd2 = new SqlCommand(Stm, con2);
            version = cmd2.ExecuteScalar().ToString();
            Assert.IsTrue(version.Contains("Microsoft"));
        }

        [TestMethod]
        public void TestBasicSqlRewriteRuleServiceWithAutoRefreshTimer()
        {
            using var repo = new SqlRewriteDbRepository(Settings.Default.ConnectionString);
            using var service = new SqlRewriteRuleService(repo, true);
            service.AutoRefreshTimerInterval = 1000;
            service.AutoRefreshRulesAndSettingsEnabled = true;
            using var con = new SqlConnection(Settings.Default.ConnectionString);
            con.Open();
            using (var cmd = new SqlCommand(Stm, con))
            {
                var version = cmd.ExecuteScalar().ToString();
                Assert.IsTrue(version.Contains("Microsoft"));
            }
            Assert.AreNotEqual(0, service.AddRule(".*", "NO MATCH", "SELECT GETDATE()"));
            Thread.Sleep(1100);
            using (var cmd = new SqlCommand(Stm, con))
            {
                var version = cmd.ExecuteScalar().ToString();
                Assert.IsTrue(version.Contains("Microsoft"));
            }
            Assert.AreNotEqual(0, service.AddRule(".*", Stm, "SELECT GETDATE()"));
            Thread.Sleep(1100);
            using (var cmd = new SqlCommand(Stm, con))
            {
                var version = cmd.ExecuteScalar().ToString();
                Assert.IsFalse(version.Contains("Microsoft"));
            }
        }

        [TestMethod]
        public void TestBasicSqlRewriteRuleServiceWithAutoRefreshTimerCausingExceptionByDroppingTable()
        {
            using var repo = new SqlRewriteDbRepository(Settings.Default.ConnectionString);
            using var service = new SqlRewriteRuleService(repo, true);
            Exception throwException = null;
            service.ExceptionDelegateEvent += e => { throwException = e;};
            using var conn = new SqlConnection(Settings.Default.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand("DROP TABLE SqlRewriteRegistry", conn);
            cmd.ExecuteNonQuery();
            service.AutoRefreshTimerInterval = 500;
            service.AutoRefreshRulesAndSettingsEnabled = true;
            Thread.Sleep(1000);
            Assert.IsNotNull(throwException);
            Assert.IsFalse(service.Enabled);
        }

        [TestMethod]
        public void TestBasicSqlRewriteRuleServiceWithAutoRefreshTimerCausingExceptionByInsertingBadRegEx()
        {
            using var repo = new SqlRewriteDbRepository(Settings.Default.ConnectionString);
            using var service = new SqlRewriteRuleService(repo, true);
            Assert.IsTrue(service.Enabled);
            Exception throwException = null;
            service.ExceptionDelegateEvent += e => { throwException = e; };
            using var conn = new SqlConnection(Settings.Default.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand($"INSERT INTO SqlRewriteRegistry (DatabaseRegEx, QueryMatchRegEx, QueryReplacementString) VALUES ('.*)', '{Stm}', 'hello')", conn);
            cmd.ExecuteNonQuery();
            service.AutoRefreshTimerInterval = 500;
            service.AutoRefreshRulesAndSettingsEnabled = true;
            Thread.Sleep(1000);
            Assert.IsNotNull(throwException);
            Assert.IsFalse(service.Enabled);
        }

        [TestMethod]
        public void TestApplySettings()
        {
            using var conn = new SqlConnection(Settings.Default.ConnectionString);
            var repo = new SqlRewriteDbRepository(conn);
            repo.RemoveAllSqlRewriteSettings();
            var settings = new SqlRewriteSettings()
            {
                MachineRegEx = Settings.Default.MachineNameMatchString,
                ProcessNameRegEx = "Program Files",
                Enabled = true,
                HashInjectionEnabled = false,
                RegExInjectionEnabled = false,
                StackFrameInjectionEnabled = false
            };
            repo.SaveSqlRewriteSettings(settings);
            using var service = new SqlRewriteRuleService(repo);
            Assert.IsFalse(service.Enabled);
            Assert.IsTrue(SqlCommandRegExProcessor.RegExInjectionEnabled);
            Assert.IsTrue(SqlCommandTextStackTraceInjector.HashInjectionEnabled);
            Assert.IsTrue(SqlCommandTextStackTraceInjector.StackInjectionEnabled);
            service.Enabled = true;
            service.ApplySettingsFromRepository();
            Assert.IsTrue(service.Enabled);
            Assert.IsFalse(SqlCommandRegExProcessor.RegExInjectionEnabled);
            Assert.IsFalse(SqlCommandTextStackTraceInjector.HashInjectionEnabled);
            Assert.IsFalse(SqlCommandTextStackTraceInjector.StackInjectionEnabled);
            repo.RemoveSqlRewriteSettings(settings.Id);
            settings.Enabled = false;
            settings.HashInjectionEnabled = true;
            settings.RegExInjectionEnabled = true;
            settings.StackFrameInjectionEnabled = true;
            repo.SaveSqlRewriteSettings(settings);
            service.ApplySettingsFromRepository();
            Assert.IsFalse(service.Enabled);
            Assert.IsTrue(SqlCommandRegExProcessor.RegExInjectionEnabled);
            Assert.IsTrue(SqlCommandTextStackTraceInjector.HashInjectionEnabled);
            Assert.IsTrue(SqlCommandTextStackTraceInjector.StackInjectionEnabled);
            service.RemoveSettings(settings.Id);
            var id = service.StoreCurrentSettings(settings.MachineRegEx, settings.ProcessNameRegEx);
            Assert.AreNotEqual(settings.Id, id);
            service.ApplySettingsFromRepository();
            Assert.IsFalse(service.Enabled);
            Assert.IsTrue(SqlCommandRegExProcessor.RegExInjectionEnabled);
            Assert.IsTrue(SqlCommandTextStackTraceInjector.HashInjectionEnabled);
            Assert.IsTrue(SqlCommandTextStackTraceInjector.StackInjectionEnabled);
            service.RemoveSettings(id);
            // ReSharper disable once AccessToDisposedClosure
            Assert.ThrowsException<SqlRewriteRuleDbRepositoryException>(() => { service.RemoveSettings(id); });
        }
    }
}
