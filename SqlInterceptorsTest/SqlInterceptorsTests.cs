using System;
using System.Data;
using System.Data.SqlClient;
using Ascentis.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlInterceptorsTest.Properties;

namespace SqlInterceptorsTest
{
    [TestClass]
    public class SqlInterceptorsTests
    {
        private const string Stm = "SELECT @@VERSION";

        [TestInitialize]
        public void TestInit()
        {
            TestUtils.InitTests();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            SqlCommandProcessor.Enabled = false;
        }

        [TestMethod]
        public void TestNoRewrite()
        {
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

        [TestMethod]
        public void TestSqlHeaderRewrite()
        {
            SqlCommandProcessor.Enabled = true;
            using (var con = new SqlConnection(Settings.Default.ConnectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(Stm, con))
                {
                    // ReSharper disable once UnusedVariable
                    var version = cmd.ExecuteScalar().ToString();
                    Assert.IsTrue(cmd.CommandText.Contains("/*AHSH=3316229661*/"));
                }
            }
        }

        [TestMethod]
        public void TestCompleteRewrite()
        {
            SqlCommandProcessor.Enabled = true;
            var rules = new SqlRewriteRule[1];
            rules[0] = new SqlRewriteRule();
            rules[0].DatabaseRegEx = ".*";
            rules[0].QueryMatchRegEx = ".*";
            rules[0].QueryReplacementString = "SELECT GETDATE()";
            SqlCommandRegExProcessor.SqlRewriteRules = rules;
            using (var con = new SqlConnection(Settings.Default.ConnectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(Stm, con))
                {
                    var version = cmd.ExecuteScalar().ToString();
                    Assert.IsFalse(version.Contains("Microsoft"));
                }
            }
        }

        [TestMethod]
        public void TestStoredProcRewrite()
        {
            SqlCommandProcessor.Enabled = true;
            //SqlCommandTextStackTraceInjector.HashInjectionEnabled = false;
            //SqlCommandTextStackTraceInjector.StackInjectionEnabled = false;
            var rules = new SqlRewriteRule[1];
            rules[0] = new SqlRewriteRule
            {
                DatabaseRegEx = ".*", 
                QueryMatchRegEx = Stm, 
                QueryReplacementString = "sp_getsqlqueueversion"
            };
            SqlCommandRegExProcessor.SqlRewriteRules = rules;
            using (var con = new SqlConnection(Settings.Default.ConnectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(Stm, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    try
                    {
                        cmd.ExecuteScalar();
                    }
                    catch (SqlException e)
                    {
                        if (!e.Message.Contains("parameter"))
                            throw;
                    }
                }
            }
        }

        [TestMethod]
        public void TestBadRegEx()
        {
            SqlCommandProcessor.Enabled = true;
            var rules = new SqlRewriteRule[1];
            rules[0] = new SqlRewriteRule();
            Assert.ThrowsException<ArgumentException>(() =>
            {
                rules[0].DatabaseRegEx = ".(*"; // Bad regex
            });
            SqlCommandRegExProcessor.SqlRewriteRules = rules;
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
