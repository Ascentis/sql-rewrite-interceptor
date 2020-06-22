using System;
using System.Data.SqlClient;
using Ascentis.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlInterceptorsTest.Properties;

namespace SqlInterceptorsTest
{
    [TestClass]
    public class SqlRewriteRuleRepositoryTests
    {
        [TestMethod]
        public void TestCreateRepository()
        {
            var repository =
                new SqlRewriteRuleDbRepository(
                    new SqlConnection(Settings.Default.ConnectionString));
            Assert.IsNotNull(repository);
        }

        [TestMethod]
        public void TestCreateEnsureTableExists()
        {
            var conn = new SqlConnection(Settings.Default.ConnectionString);
            conn.Open();
            var dropTable = new SqlCommand("DROP TABLE SqlRewriteRegistry", conn);
            try
            {
                dropTable.ExecuteNonQuery();
            }
            catch (Exception)
            {
                // Ignore exception attempting to drop table if it doesn't exist
            }
            var cmdCheckTable = new SqlCommand("SELECT OBJECT_ID (N'SqlRewriteRegistry', N'U') obj_id", conn);
            var tableId = cmdCheckTable.ExecuteScalar();
            Assert.IsTrue(tableId is DBNull);
            var repository = new SqlRewriteRuleDbRepository(conn);
            Assert.IsNotNull(repository);
            tableId = cmdCheckTable.ExecuteScalar();
            Assert.IsFalse(tableId is DBNull);
        }
    }
}
