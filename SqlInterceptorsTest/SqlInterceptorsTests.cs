﻿using System;
using System.Data;
using System.Data.SqlClient;
using Ascentis.Infrastructure.SqlInterceptors.Injectors;
using Ascentis.Infrastructure.SqlInterceptors.Model;
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
            SqlCommandInterceptor.Enabled = false;
            SqlCommandTextStackTraceInjector.StackFrameIgnorePrefixes = "";
        }

        [TestMethod]
        public void TestNoRewrite()
        {
            using var con = new SqlConnection(Settings.Default.ConnectionString);
            con.Open();
            using var cmd = new SqlCommand(Stm, con);
            var version = cmd.ExecuteScalar().ToString();
            Assert.IsTrue(version.Contains("Microsoft"));

        }

        [TestMethod]
        public void TestSqlHeaderRewrite()
        {
            SqlCommandInterceptor.Enabled = true;
            using var con = new SqlConnection(Settings.Default.ConnectionString);
            con.Open();
            using var cmd = new SqlCommand(Stm, con);
            // ReSharper disable once UnusedVariable
            var version = cmd.ExecuteScalar().ToString();
            Assert.IsTrue(cmd.CommandText.Contains("/*AHSH=3316229661*/"));
        }

        [TestMethod]
        public void TestCompleteRewrite()
        {
            SqlCommandInterceptor.Enabled = true;
            var rules = new SqlRewriteRule[1];
            rules[0] = new SqlRewriteRule();
            rules[0].DatabaseRegEx = ".*";
            rules[0].QueryMatchRegEx = $"(.*){Stm}(.*)";
            rules[0].QueryReplacementString = $"$1SELECT GETDATE()$2\r\n{SqlCommandRegExProcessor.RegReplacementIndicator}";
            SqlCommandRegExProcessor.SqlRewriteRules = rules;
            using var con = new SqlConnection(Settings.Default.ConnectionString);
            con.Open();
            using var cmd = new SqlCommand(Stm, con);
            Assert.IsTrue(cmd.CommandText.EndsWith(SqlCommandRegExProcessor.RegReplacementIndicator), $"SQL should end with {SqlCommandRegExProcessor.RegReplacementIndicator}");
            var version = cmd.ExecuteScalar().ToString();
            Assert.IsFalse(version.Contains("Microsoft"));
        }

        [TestMethod]
        public void TestStoredProcRewrite()
        {
            SqlCommandInterceptor.Enabled = true;
            var rules = new SqlRewriteRule[1];
            rules[0] = new SqlRewriteRule
            {
                DatabaseRegEx = ".*", 
                QueryMatchRegEx = Stm, 
                QueryReplacementString = "sp_getsqlqueueversion"
            };
            SqlCommandRegExProcessor.SqlRewriteRules = rules;
            using var con = new SqlConnection(Settings.Default.ConnectionString);
            con.Open();
            using var cmd = new SqlCommand(Stm, con);
            cmd.CommandType = CommandType.StoredProcedure;
            try
            {
                cmd.ExecuteScalar();
                throw new Exception("Expecting exception missing parameter executing stored proc");
            }
            catch (SqlException e)
            {
                if (!e.Message.Contains("parameter"))
                    throw;
            }
        }

        [TestMethod]
        public void TestBadRegEx()
        {
            SqlCommandInterceptor.Enabled = true;
            var rules = new SqlRewriteRule[1];
            rules[0] = new SqlRewriteRule();
            Assert.ThrowsException<ArgumentException>(() =>
            {
                rules[0].DatabaseRegEx = ".(*"; // Bad regex
            });
            SqlCommandRegExProcessor.SqlRewriteRules = rules;
            using var con = new SqlConnection(Settings.Default.ConnectionString);
            con.Open();
            using var cmd = new SqlCommand(Stm, con);
            var version = cmd.ExecuteScalar().ToString();
            Assert.IsTrue(version.Contains("Microsoft"));
        }

        [TestMethod]
        public void TestSqlStackFrameInjection()
        {
            SqlCommandInterceptor.Enabled = true;
            using var con = new SqlConnection(Settings.Default.ConnectionString);
            con.Open();
            SqlCommandTextStackTraceInjector.StackFrameIgnorePrefixes = @"  [Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices].
  [Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter].  


  [Microsoft.VisualStudio.TestPlatform.TestFramework].  ";
            using var cmd = new SqlCommand(Stm, con);
            // ReSharper disable once UnusedVariable
            var version = cmd.ExecuteScalar().ToString();
            Assert.IsFalse(cmd.CommandText.Contains("[Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter].Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute.Execute"));
            Assert.IsTrue(cmd.CommandText.Contains("[SqlInterceptorsTest].SqlInterceptorsTest.SqlInterceptorsTests."));
            Assert.IsFalse(cmd.CommandText.Contains("[Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter].Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter"));
        }
    }
}
