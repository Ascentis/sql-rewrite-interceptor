using System.Data.SqlClient;
using SqlInterceptorsTest.Properties;

namespace SqlInterceptorsTest
{
 public class TestUtils
    {
        public static void DropTables()
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
                using (var truncateTable = new SqlCommand("TRUNCATE TABLE SqlRewriteInjectorSettings", conn))
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
    }
}
