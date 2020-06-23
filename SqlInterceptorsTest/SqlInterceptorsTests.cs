﻿using System;
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
            SqlInterceptorsInit.Init();
            RegisterSqlCommandInjectors.Register();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            SqlCommandProcessorBase.Enabled = false;
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
            SqlCommandProcessorBase.Enabled = true;
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
            SqlCommandProcessorBase.Enabled = true;
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
        public void TestBadRegEx()
        {
            SqlCommandProcessorBase.Enabled = true;
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
