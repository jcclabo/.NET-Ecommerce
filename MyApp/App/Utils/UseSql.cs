using Microsoft.Data.SqlClient;

namespace MyApp.App.Utils
{
    public static class UseSql
    {
        public static (SqlConnection conn, SqlCommand cmd) ConnAndCmd(string sql) {
            string connStr = AppSettings.GetString("DbConnectionString");
            SqlConnection conn = new SqlConnection(connStr);
            conn.Open();
            SqlCommand cmd = new SqlCommand(sql, conn);
            return (conn, cmd);
        }

        public static (SqlConnection conn, SqlCommand cmd, SqlTransaction transaction) SetUpTransaction(string sql) {
            string connStr = AppSettings.GetString("DbConnectionString");
            SqlConnection conn = new SqlConnection(connStr);
            SqlTransaction transaction;
            conn.Open();
            transaction = conn.BeginTransaction();
            SqlCommand cmd = new SqlCommand(sql, conn, transaction);
            return (conn, cmd, transaction);
        }

        public static void Close(SqlConnection conn) {
            conn.Close();
            conn.Dispose();
        }

        public static void Close(SqlConnection conn, SqlCommand cmd) {
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public static void Close(SqlConnection conn, SqlCommand cmd, SqlDataReader reader) {
            reader.Close();
            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }
    }
}
