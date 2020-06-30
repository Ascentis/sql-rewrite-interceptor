using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Ascentis.Infrastructure.DBRepository;
using Ascentis.Infrastructure.SqlInterceptors.Model;
using Ascentis.Infrastructure.SqlInterceptors.Model.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlInterceptorsTest.Properties;

namespace SqlInterceptorsTest
{
    [TestClass]
    public class SqlRewriteRepositoryTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestUtils.InitTests();
        }

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
                try
                {
                    repository.RemoveSqlRewriteRule(item.Id);
                }
                catch (Exception)
                {
                    // Ignore exceptions trying to remove to cleanup
                }
            }
        }

        private static void SettingsExists(IEnumerable<SqlRewriteSettings> items, SqlRewriteSettings item)
        {
            foreach (var loadedItem in items)
            {
                if (loadedItem.Id != item.Id)
                    continue;
                Assert.AreEqual(item.Id, loadedItem.Id);
                Assert.AreEqual(item.ProcessNameRegEx, loadedItem.ProcessNameRegEx);
                Assert.AreEqual(item.MachineRegEx, loadedItem.MachineRegEx);
                Assert.AreEqual(item.Enabled, loadedItem.Enabled);
                Assert.AreEqual(item.HashInjectionEnabled, loadedItem.HashInjectionEnabled);
                Assert.AreEqual(item.RegExInjectionEnabled, loadedItem.RegExInjectionEnabled);
                Assert.AreEqual(item.StackFrameInjectionEnabled, loadedItem.StackFrameInjectionEnabled);
                Assert.AreEqual(item.CallStackEntriesToReport, loadedItem.CallStackEntriesToReport);
                Assert.AreEqual(item.StackFrameIgnorePrefixes, loadedItem.StackFrameIgnorePrefixes);
                return;
            }
            throw new Exception("Settings entry doesn't exist");
        }

        [TestMethod]
        public void TestSaveAndRemoveSettings()
        {
            var repository = new SqlRewriteDbRepository(new SqlConnection(Settings.Default.ConnectionString));
            var settings = new SqlRewriteSettings
            {
                Id = 0, // This will cause insertion
                ProcessNameRegEx = ".*",
                MachineRegEx = ".*",
                StackFrameIgnorePrefixes = "[mscorlib].\r\nSqlInterceptorsTests."
            };
            repository.SaveSqlRewriteSettings(settings);
            try
            {
                Assert.AreNotEqual(0, settings.Id);
                var items = repository.LoadSqlRewriteSettings();
                SettingsExists(items, settings);
                repository.RemoveSqlRewriteSettings(settings.Id);
                items = repository.LoadSqlRewriteSettings();
                Assert.ThrowsException<Exception>(() => { SettingsExists(items, settings); });
            }
            finally
            {
                try
                {
                    repository.RemoveSqlRewriteSettings(settings.Id);
                }
                catch (Exception)
                {
                    // Ignore exception
                }
            }
        }

        [TestMethod]
        public void TestRegExCacheKeyEquality()
        {
            var regExCacheKey = new RegExCacheKey("A key", RegexOptions.Singleline);
            var regExCacheKey2 = new RegExCacheKey("A key", RegexOptions.Singleline);
            Assert.AreEqual(regExCacheKey2, regExCacheKey);
            regExCacheKey = new RegExCacheKey("A key", RegexOptions.Singleline);
            regExCacheKey2 = new RegExCacheKey("Another key", RegexOptions.Singleline);
            Assert.AreNotEqual(regExCacheKey2, regExCacheKey);
            regExCacheKey = new RegExCacheKey("A key", RegexOptions.Multiline);
            regExCacheKey2 = new RegExCacheKey("A key", RegexOptions.Singleline);
            Assert.AreNotEqual(regExCacheKey2, regExCacheKey);
        }
    }
}
