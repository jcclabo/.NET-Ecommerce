using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Data.SqlClient;
using MyApp.App.ErrorHandling;
using MyApp.App.Utils;
using System.Data;
using System.Text.Json;

namespace MyApp.App.Biz
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string ImgUrl { get; set; }
        public decimal Cost { get; set; }
        public decimal Price { get; set; }
        public string Descr { get; set; }
        public string Status { get; set; }
        public DateTime InsWhen { get; set; }
        public DateTime? UpdWhen { get; set; }

        public string Error { get; private set; }
        public string[] InputErrors { get; private set; }
        private const int requiredPlusHeader = 5 + 1; // defines InputErrors size

        public Product() {
            ProductId = 0;
            Name = "";
            ImgUrl = "";
            Cost = 0;
            Price = 0;
            Descr = "";
            Status = "";
            InsWhen = DateTime.MinValue;
            Error = "";
            InputErrors = new string[requiredPlusHeader];
        }

        public Product GetById(int id) {
            string sql = @"SELECT name, imgUrl, cost, price, descr, status, insWhen, updWhen FROM products WHERE productId = @productId";

            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            sqlCmd.Parameters.Add("@productId", SqlDbType.Int).Value = id;

            SqlDataReader reader = sqlCmd.ExecuteReader();
            Product prod = new Product();
            prod.ProductId = id;
            int index = 0;
            try {
                reader.Read();
                prod.Name = reader.GetString(index++);
                prod.ImgUrl = reader.GetString(index++);
                prod.Cost = reader.GetDecimal(index++);
                prod.Price = reader.GetDecimal(index++);
                prod.Descr = reader.GetString(index++);
                prod.Status = reader.GetString(index++);
                prod.InsWhen = reader.GetDateTime(index++);
                object sqlDateTime = reader[index++];
                if (sqlDateTime == DBNull.Value) prod.UpdWhen = null;
                else prod.UpdWhen = Convert.ToDateTime(sqlDateTime);
                return prod;
            } catch(Exception ex) {
                Console.WriteLine(ex.ToString());
                return new Product() { Error = "Unable to find product in the database", ProductId = id};
            } finally {
                UseSql.Close(conn, sqlCmd, reader);
            }
        }

        public bool Validate() {
            string errorInit = "Please make the following changes:\r\n";
            string error = errorInit;

            if (string.IsNullOrWhiteSpace(Name)) 
                error += "• Enter the product name\r\n";
            if (string.IsNullOrWhiteSpace(ImgUrl))
                error += "• Enter an image Url\r\n";
            if (Cost == 0)
                error += "• Enter a cost greater than 0\r\n";
            if (Price == 0)
                error += "• Enter a price greater than 0\r\n";
            if (string.IsNullOrWhiteSpace(Descr))
                error += "• Enter a description\r\n";

            if (error != errorInit) {
                InputErrors = error.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                return false;
            }
            return true;
        }

        public bool Insert() {
            // validate product fields
            if (!Validate())
                return false;

            string sql = @"INSERT INTO products (name, imgUrl, cost, price, descr, status, insWhen, updWhen) " +
                    "VALUES (@name, @url, @cost, @price, @descr, @status, @insWhen, @updWhen)";

            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            sqlCmd.Parameters.Add("@name", SqlDbType.VarChar).Value = Name;
            sqlCmd.Parameters.Add("@url", SqlDbType.VarChar).Value = ImgUrl;
            sqlCmd.Parameters.Add("@cost", SqlDbType.Money).Value = Cost;
            sqlCmd.Parameters.Add("@price", SqlDbType.Money).Value = Price;
            sqlCmd.Parameters.Add("@descr", SqlDbType.VarChar).Value = Descr;
            sqlCmd.Parameters.Add("@status", SqlDbType.VarChar).Value = "active";
            sqlCmd.Parameters.Add("@insWhen", SqlDbType.DateTime).Value = DateTime.Now;
            sqlCmd.Parameters.Add("@updWhen", SqlDbType.DateTime).Value = DBNull.Value;

            try {
                int affected = sqlCmd.ExecuteNonQuery();
                return true;
            } catch(Exception ex) {
                Console.WriteLine(ex.ToString());
                Error = "Unable to insert product into the database";
                return false;
            } finally {
                UseSql.Close(conn, sqlCmd);
            }
        }

        public bool Update(int id) {
            // validate product fields
            if (!Validate())
                return false;

            string sql = @"UPDATE products " +
                    "SET name=@name, imgUrl=@url, cost=@cost, price=@price, descr=@descr, status=@status, updWhen=@upd " +
                    "WHERE productId=@productId";

            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            sqlCmd.Parameters.Add("@productId", SqlDbType.Int).Value = id;
            sqlCmd.Parameters.Add("@name", SqlDbType.VarChar).Value = Name;
            sqlCmd.Parameters.Add("@url", SqlDbType.VarChar).Value = ImgUrl;
            sqlCmd.Parameters.Add("@cost", SqlDbType.Money).Value = Cost;
            sqlCmd.Parameters.Add("@price", SqlDbType.Money).Value = Price;
            sqlCmd.Parameters.Add("@descr", SqlDbType.VarChar).Value = Descr;
            sqlCmd.Parameters.Add("@status", SqlDbType.VarChar).Value = Status;
            sqlCmd.Parameters.Add("@upd", SqlDbType.DateTime).Value = DateTime.Now;

            try {
                int affected = sqlCmd.ExecuteNonQuery();
                return true;
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                Error = "Unable to update product in the database";
                return false;
            } finally {
                UseSql.Close(conn, sqlCmd);
            }
        }

        public static List<Product> GetAllProducts() {
            string sql = @"SELECT productId, name, imgUrl, cost, price, descr, status, insWhen, updWhen FROM products";
            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            SqlDataReader? reader = null;
            List<Product> products = new List<Product>();
            try {
                reader = sqlCmd.ExecuteReader();
                while (reader.Read()) {
                    Product prod = new Product();
                    int index = 0;
                    prod.ProductId = reader.GetInt32(index++);
                    prod.Name = reader.GetString(index++);
                    prod.ImgUrl = reader.GetString(index++);
                    prod.Cost = reader.GetDecimal(index++);
                    prod.Price = reader.GetDecimal(index++);
                    prod.Descr = reader.GetString(index++);
                    prod.Status = reader.GetString(index++);
                    prod.InsWhen = reader.GetDateTime(index++);
                    object sqlDateTime = reader[index++];
                    if (sqlDateTime == DBNull.Value) prod.UpdWhen = null;
                    else prod.UpdWhen = Convert.ToDateTime(sqlDateTime);
                    products.Add(prod);
                }
                return products;
            } finally {
                if (reader != null) reader.Close();
                UseSql.Close(conn, sqlCmd);
            }
        }

        public static List<Product> GetActiveProducts() {
            string sql = @"SELECT productId, name, imgUrl, cost, price, descr, status, insWhen, updWhen FROM products WHERE status='active'";
            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            SqlDataReader? reader = null;
            List<Product> products = new List<Product>();
            try {
                reader = sqlCmd.ExecuteReader();
                while (reader.Read()) {
                    Product prod = new Product();
                    int index = 0;
                    prod.ProductId = reader.GetInt32(index++);
                    prod.Name = reader.GetString(index++);
                    prod.ImgUrl = reader.GetString(index++);
                    prod.Cost = reader.GetDecimal(index++);
                    prod.Price = reader.GetDecimal(index++);
                    prod.Descr = reader.GetString(index++);
                    prod.Status = reader.GetString(index++);
                    prod.InsWhen = reader.GetDateTime(index++);
                    object sqlDateTime = reader[index++];
                    if (sqlDateTime == DBNull.Value) prod.UpdWhen = null;
                    else prod.UpdWhen = Convert.ToDateTime(sqlDateTime);
                    products.Add(prod);
                }
                return products;
            } finally {
                if (reader != null) reader.Close();
                UseSql.Close(conn, sqlCmd);
            }
        }

        //public static List<Product> GetActiveProducts(int amt, int pg) {
        //    List<Product> list = new List<Product>();
        //    Product prod = new Product();
        //    int id = 1000; //db starts at 1000
        //    int offset = (pg - 1) * amt;
        //    id += offset;
        //    int count = 0;
        //    while (count < amt) {
        //        prod = prod.GetById(id);
        //        if (prod.Status == "active") {
        //            list.Add(prod);
        //            count++;
        //        }
        //        id++;
        //    }
        //    return list;
        //}

        /// <summary>
        /// Excludes product cost value
        /// </summary>
        /// <returns> json string </returns>
        public string Serialize() {
            Product prod = this;
            return JsonSerializer.Serialize(prod);
        }

        /// <summary>
        /// Excludes product cost value
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public Product Deserialize(string json) {
            Product prod = JsonSerializer.Deserialize<Product>(json);
            if (prod != null) {
                return prod;
            }
            throw new AppException("json string resolved to null");
        }
    }
}
