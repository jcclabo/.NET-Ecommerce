using Microsoft.Data.SqlClient;
using MyApp.App.Utils;
using System.Data;

namespace MyApp.App.Biz
{
    public class OrderLine
    {
        public int OrderLineId { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime OrderDate { get; set; }

        public string ImgUrl { get; set; }
        public string Descr { get; set; }

        public OrderLine() {
            OrderLineId = 0;
            OrderId = 0;
            CustomerId = 0;
            ProductId = 0;
            Name = "";
            Qty = 0;
            UnitPrice = 0;
            OrderDate = DateTime.MinValue;
            ImgUrl = "";
            Descr = "";
        }

        public static OrderLine GetById(int id) {
            string sql = @"SELECT orderId, customerId, productId, name, qty, unitPrice, orderDate FROM orderLines WHERE orderLineId = @orderLineId";

            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            sqlCmd.Parameters.Add("@orderLineId", SqlDbType.Int).Value = id;

            SqlDataReader reader = sqlCmd.ExecuteReader();
            OrderLine line = new OrderLine();
            line.OrderLineId = id;
            int index = 0;
            try {
                reader.Read();
                line.OrderId = reader.GetInt32(index++);
                line.CustomerId = reader.GetInt32(index++);
                line.ProductId = reader.GetInt32(index++);
                line.Name = reader.GetString(index++);
                line.Qty = reader.GetInt32(index++);
                line.UnitPrice = reader.GetDecimal(index++);
                line.OrderDate = reader.GetDateTime(index++);
                return line;
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return new OrderLine();
            } finally {
                UseSql.Close(conn, sqlCmd, reader);
            }
        }

        /// <summary>
        /// OrderLines should only be inserted by an order
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public bool Insert(SqlConnection conn, SqlTransaction transaction) {
            string sql = @"INSERT INTO orderLines (orderId, customerId, productId, name, qty, unitPrice, orderDate) " +
                "VALUES (@orderId, @customerId, @productId, @name, @qty, @unitPrice, @orderDate)";

            SqlCommand sqlCmd = new SqlCommand(sql, conn, transaction);
            sqlCmd.Parameters.Add("@orderId", SqlDbType.Int).Value = OrderId;
            sqlCmd.Parameters.Add("@customerId", SqlDbType.Int).Value = CustomerId;
            sqlCmd.Parameters.Add("@productId", SqlDbType.Int).Value = ProductId;
            sqlCmd.Parameters.Add("@name", SqlDbType.VarChar).Value = Name;
            sqlCmd.Parameters.Add("@qty", SqlDbType.Int).Value = Qty;
            sqlCmd.Parameters.Add("@unitPrice", SqlDbType.Money).Value = UnitPrice;
            sqlCmd.Parameters.Add("@orderDate", SqlDbType.DateTime).Value = DateTime.Now;

            try {
                int affected = sqlCmd.ExecuteNonQuery();
                return true;
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public static List<OrderLine> GetList() {
            string sql = @"SELECT orderId, customerId, productId, name, qty, unitPrice, orderDate FROM orderLines";
            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            SqlDataReader? reader = null;
            List<OrderLine> orderLines = new List<OrderLine>();
            try {
                reader = sqlCmd.ExecuteReader();
                while (reader.Read()) {
                    OrderLine line = new OrderLine();
                    int index = 0;
                    line.OrderId = reader.GetInt32(index++);
                    line.CustomerId = reader.GetInt32(index++);
                    line.ProductId = reader.GetInt32(index++);
                    line.Name = reader.GetString(index++);
                    line.Qty = reader.GetInt32(index++);
                    line.UnitPrice = reader.GetDecimal(index++);
                    line.OrderDate = reader.GetDateTime(index++);
                    orderLines.Add(line);
                }
                return orderLines;
            } finally {
                UseSql.Close(conn, sqlCmd, reader);
            }
        }

        public static List<OrderLine> GetList(int orderId) {
            string sql = @"SELECT customerId, productId, name, qty, unitPrice, orderDate FROM orderLines where orderId=@orderId";
            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            sqlCmd.Parameters.Add("@orderId", SqlDbType.Int).Value = orderId;
            SqlDataReader? reader = null;
            List<OrderLine> orderLines = new List<OrderLine>();
            try {
                reader = sqlCmd.ExecuteReader();
                while (reader.Read()) {
                    OrderLine line = new OrderLine();
                    int index = 0;
                    line.OrderId = orderId;
                    line.CustomerId = reader.GetInt32(index++);
                    line.ProductId = reader.GetInt32(index++);
                    line.Name = reader.GetString(index++);
                    line.Qty = reader.GetInt32(index++);
                    line.UnitPrice = reader.GetDecimal(index++);
                    line.OrderDate = reader.GetDateTime(index++);
                    // set img url
                    Product prod = new Product();
                    prod = prod.GetById(line.ProductId);
                    line.ImgUrl = prod.ImgUrl;
                    orderLines.Add(line);
                }
                return orderLines;
            } finally {
                UseSql.Close(conn, sqlCmd, reader);
            }
        }



    }
}
