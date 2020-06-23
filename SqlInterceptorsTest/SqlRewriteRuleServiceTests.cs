﻿using System;
using System.Data.SqlClient;
using System.Threading;
using Ascentis.Infrastructure;
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
            TestUtils.DropTables();
        }

        [TestMethod]
        public void TestCreateSqlRewriteRuleService()
        {
            using (var conn = new SqlConnection(Settings.Default.ConnectionString))
            {
                using (var service = new SqlRewriteRuleService(new SqlRewriteDbRepository(conn)))
                {
                    Assert.IsNotNull(service);
                }
            }
        }

        [TestMethod]
        public void TestBasicSqlRewriteRuleService()
        {
            using (var conn = new SqlConnection(Settings.Default.ConnectionString))
            {
                var repo = new SqlRewriteDbRepository(conn);
                var rule = new SqlRewriteRule
                {
                    DatabaseRegEx = ".*", QueryMatchRegEx = Stm, QueryReplacementString = "SELECT GETDATE()"
                };
                repo.SaveSqlRewriteRule(rule);
                using (var service = new SqlRewriteRuleService(repo, true))
                {
                    using (var con = new SqlConnection(Settings.Default.ConnectionString))
                    {
                        con.Open();
                        using (var cmd = new SqlCommand(Stm, con))
                        {
                            var version = cmd.ExecuteScalar().ToString();
                            Assert.IsFalse(version.Contains("Microsoft"));
                        }
                    }
                    service.RemoveRule(rule.Id);
                    service.RefreshRulesFromRepository();
                    using (var con = new SqlConnection(Settings.Default.ConnectionString))
                    {
                        con.Open();
                        using (var cmd = new SqlCommand(Stm, con))
                        {
                            var version = cmd.ExecuteScalar().ToString();
                            Assert.IsTrue(version.Contains("Microsoft"));
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void TestBasicSqlRewriteRuleServiceWithAutoRefreshTimer()
        {
            using (var repo = new SqlRewriteDbRepository(Settings.Default.ConnectionString))
            {
                using (var service = new SqlRewriteRuleService(repo, true))
                {
                    service.AutoRefreshTimerInterval = 1000;
                    service.AutoRefreshRulesEnabled = true;
                    using (var con = new SqlConnection(Settings.Default.ConnectionString))
                    {
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
                }
            }
        }

        [TestMethod]
        public void TestBasicSqlRewriteRuleServiceWithAutoRefreshTimerCausingExceptionByDroppingTable()
        {
            using (var repo = new SqlRewriteDbRepository(Settings.Default.ConnectionString))
            {
                using (var service = new SqlRewriteRuleService(repo, true))
                {
                    Exception throwException = null;
                    service.ExceptionDelegateEvent += e => { throwException = e;};
                    using (var conn = new SqlConnection(Settings.Default.ConnectionString))
                    {
                        conn.Open();
                        using (var cmd = new SqlCommand("DROP TABLE SqlRewriteRegistry", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    service.AutoRefreshTimerInterval = 500;
                    service.AutoRefreshRulesEnabled = true;
                    Thread.Sleep(1000);
                    Assert.IsNotNull(throwException);
                    Assert.IsFalse(service.Enabled);
                }
            }
        }

        [TestMethod]
        public void TestBasicSqlRewriteRuleServiceWithAutoRefreshTimerCausingExceptionByInsertingBadRegEx()
        {
            using (var repo = new SqlRewriteDbRepository(Settings.Default.ConnectionString))
            {
                using (var service = new SqlRewriteRuleService(repo, true))
                {
                    Assert.IsTrue(service.Enabled);
                    Exception throwException = null;
                    service.ExceptionDelegateEvent += e => { throwException = e; };
                    using (var conn = new SqlConnection(Settings.Default.ConnectionString))
                    {
                        conn.Open();
                        using (var cmd = new SqlCommand($"INSERT INTO SqlRewriteRegistry (DatabaseRegEx, QueryMatchRegEx, QueryReplacementString) VALUES ('.*)', '{Stm}', 'hello')", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    service.AutoRefreshTimerInterval = 500;
                    service.AutoRefreshRulesEnabled = true;
                    Thread.Sleep(1000);
                    Assert.IsNotNull(throwException);
                    Assert.IsFalse(service.Enabled);
                }
            }
        }
    }
}
