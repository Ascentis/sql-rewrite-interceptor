using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Ascentis.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlInterceptorsTest.Properties;

namespace SqlInterceptorsTest
{
    [TestClass]
    public class SqlRewriteRepositoryTests
    {
        [TestMethod]
        public void TestCreateRepositoryWithConnectionString()
        {
            using(var repository = new SqlRewriteDbRepository(Settings.Default.ConnectionString))
                Assert.IsNotNull(repository);
        }

        [TestMethod]
        public void TestCreateEnsureTableExists()
        {
            var conn = new SqlConnection(Settings.Default.ConnectionString);
            conn.Open();
            using (var dropTable = new SqlCommand("DROP TABLE SqlRewriteRegistry", conn))
            {
                try
                {
                    dropTable.ExecuteNonQuery();
                }
                catch (Exception)
                {
                    // Ignore exception attempting to drop table if it doesn't exist
                }
            }

            using (var cmdCheckTable = new SqlCommand("SELECT OBJECT_ID (N'SqlRewriteRegistry', N'U') obj_id", conn))
            {
                var tableId = cmdCheckTable.ExecuteScalar();
                Assert.IsTrue(tableId is DBNull);
                var repository = new SqlRewriteDbRepository(conn);
                Assert.IsNotNull(repository);
                tableId = cmdCheckTable.ExecuteScalar();
                Assert.IsFalse(tableId is DBNull);
            }
        }

        private static void ItemExists(IEnumerable<SqlRewriteRule> items, SqlRewriteRule item)
        {
            foreach (var loadedItem in items)
            {
                if (loadedItem.Id != item.Id)
                    continue;
                Assert.AreEqual(item.Id, loadedItem.Id);
                Assert.AreEqual(item.DatabaseRegEx, loadedItem.DatabaseRegEx);
                Assert.AreEqual(item.QueryMatchRegEx, loadedItem.QueryMatchRegEx);
                Assert.AreEqual(item.QueryReplacementString, loadedItem.QueryReplacementString);
                Assert.AreEqual(item.RegExOptions, loadedItem.RegExOptions);
                return;
            }
            throw new Exception("Item doesn't exist");
        }

        [TestMethod]
        public void TestSaveAndRemoveItem()
        {
            var repository = new SqlRewriteDbRepository(new SqlConnection(Settings.Default.ConnectionString));
            var item = new SqlRewriteRule
            {
                Id = 0, // This will cause insertion
                DatabaseRegEx = ".*",
                QueryMatchRegEx = "SELECT @@VERSION",
                QueryReplacementString = "SELECT DBDATE()",
                RegExOptions = 0
            };
            repository.SaveSqlRewriteRule(item);
            try
            {
                Assert.AreNotEqual(0, item.Id);
                var items = repository.LoadSqlRewriteRules();
                ItemExists(items, item);
                repository.RemoveSqlRewriteRule(item.Id);
                items = repository.LoadSqlRewriteRules();
                Assert.ThrowsException<Exception>(() => { ItemExists(items, item); });
            }
            finally
            {
                repository.RemoveSqlRewriteRule(item.Id);
            }
        }

        [TestMethod]
        public void TestSaveAndRemoveSettings()
        {
            var repository = new SqlRewriteDbRepository(new SqlConnection(Settings.Default.ConnectionString));
            var settings = new SqlRewriteSettings
            {
                Id = 0, // This will cause insertion
                ProcessNameRegEx = ".*",
                MachineRegEx = ".*"
            };
            repository.SaveSqlRewriteSettings(settings);
            try
            {
                Assert.AreNotEqual(0, settings.Id);
                /*var items = repository.LoadSqlRewriteRules();
                ItemExists(items, item);
                repository.RemoveSqlRewriteRule(item.Id);
                items = repository.LoadSqlRewriteRules();
                Assert.ThrowsException<Exception>(() => { ItemExists(items, item); });*/
            }
            finally
            {
                //repository.RemoveSqlRewriteRule(item.Id);
            }
        }
    }
}
