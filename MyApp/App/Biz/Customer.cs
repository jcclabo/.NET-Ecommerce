using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using MyApp.App.ErrorHandling;
using MyApp.App.Utils;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace MyApp.App.Biz
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string AdrL1 { get; set; }
        public string AdrL2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string HashedPswd { get; private set; }
        public DateTime InsWhen { get; set; } 
        public DateTime? UpdWhen { get; set; }

        public List<Order> Orders { get; set; }
        public string Error { get; private set; }
        public string[] InputErrors { get; private set; }
        private const int requiredPlusHeader = 7 + 1; // defines InputErrors size

        public Customer() {
            CustomerId = 0;
            First = "";
            Last = "";
            Email = "";
            Phone = "";
            AdrL1 = "";
            AdrL2 = "";
            City = "";
            State = "";
            Zip = "";
            HashedPswd = "";
            InsWhen = DateTime.MinValue;
            UpdWhen = DateTime.MinValue;
            Orders = new List<Order>();
            Error = "";
            InputErrors = new string[requiredPlusHeader];
        }

        public static Customer GetById(int id) {
            string sql = @"SELECT first, last, email, phone, adrL1, adrL2, city, state, zip, hashedPswd, insWhen, updWhen " +
                "FROM customers WHERE customerId = @customerId";

            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            sqlCmd.Parameters.Add("@customerId", SqlDbType.Int).Value = id;

            var reader = sqlCmd.ExecuteReader();
            App.Biz.Customer customer = new();
            customer.CustomerId = id;
            int index = 0;
            try {
                reader.Read();
                customer.First = reader.GetString(index++);
                customer.Last = reader.GetString(index++);
                customer.Email = reader.GetString(index++);
                customer.Phone = reader.GetString(index++);
                customer.AdrL1 = reader.GetString(index++);
                customer.AdrL2 = reader.GetString(index++);
                customer.City = reader.GetString(index++);
                customer.State = reader.GetString(index++);
                customer.Zip = reader.GetString(index++);
                customer.HashedPswd = reader.GetString(index++);
                customer.InsWhen = reader.GetDateTime(index++);
                object sqlDateTime = reader[index++];
                if (sqlDateTime == DBNull.Value) customer.UpdWhen = null;
                else customer.UpdWhen = Convert.ToDateTime(sqlDateTime);
                reader.Close();
                return customer;
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return new Customer() { Error = "Unable to find customer in the database", CustomerId = id };
            } finally {
                UseSql.Close(conn, sqlCmd, reader);
            }
        }

        public Customer GetByEmail(string email) {
            string sql = @"SELECT customerId, first, last, phone, adrL1, adrL2, city, state, zip, hashedPswd, insWhen, updWhen " +
                "FROM customers WHERE email = @email";

            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            sqlCmd.Parameters.Add("@email", SqlDbType.VarChar).Value = email;

            var reader = sqlCmd.ExecuteReader();
            App.Biz.Customer customer = new();
            customer.Email = email.ToLower(new CultureInfo("en-US", false));
            int index = 0;
            try {
                reader.Read();
                customer.CustomerId = reader.GetInt32(index++);
                customer.First = reader.GetString(index++);
                customer.Last = reader.GetString(index++);
                customer.Phone = reader.GetString(index++);
                customer.AdrL1 = reader.GetString(index++);
                customer.AdrL2 = reader.GetString(index++);
                customer.City = reader.GetString(index++);
                customer.State = reader.GetString(index++);
                customer.Zip = reader.GetString(index++);
                customer.HashedPswd = reader.GetString(index++);
                customer.InsWhen = reader.GetDateTime(index++);
                object sqlDateTime = reader[index++];
                if (sqlDateTime == DBNull.Value) customer.UpdWhen = null;
                else customer.UpdWhen = Convert.ToDateTime(sqlDateTime);
                reader.Close();
                return customer;
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return new Customer() { Error = "Unable to find customer in the database" };
            } finally {
                UseSql.Close(conn, sqlCmd, reader);
            }
        }

        private string ValidateNewPassword(string error, string plainTextPswd, string repeatPlainPswd) {
            bool capital = false;
            bool lower = false;
            bool number = false;
            bool okLength = false;

            if (plainTextPswd.Any(c => c >= 'A' && c <= 'Z'))
                capital = true;
            if (plainTextPswd.Any(c => c >= 'a' && c <= 'z'))
                lower = true;
            if (plainTextPswd.Any(c => c >= '0' && c <= '9'))
                number = true;
            if (plainTextPswd.Length >= 8 || repeatPlainPswd.Length >= 8)
                okLength = true;

            if (!capital || !lower || !number || !okLength)
                error += "• Your password must be at least 8 characters long, " +
                   "contain at least one number and have a mixture of uppercase and lowercase letters.\r\n";

            if (plainTextPswd.CompareTo(repeatPlainPswd) != 0)
                error += "• Enter the same password in both fields\r\n";

            return error;
        }

        private bool ValidateInsert(string plainTextPswd, string repeatPlainPswd) {
            string errorInit = "Please make the following changes:\r\n";
            string error = errorInit;

            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@') || !Email.Contains('.'))
                error += "• Enter a valid email address\r\n";
            // database uses a unique index for email
            error = ValidateNewPassword(error, plainTextPswd, repeatPlainPswd);

            if (error != errorInit) {
                InputErrors = error.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                return false;
            } else {
                return true;
            }
        }

        private string HashPassword(Customer customer, string password) {
            PasswordHasher<Customer> passwordHasher = new PasswordHasher<Customer>();
            return passwordHasher.HashPassword(customer, password);
        }

        public bool Insert(string plainTextPswd, string repeatPlainPswd) {
            if (!ValidateInsert(plainTextPswd, repeatPlainPswd))
                return false;

            HashedPswd = HashPassword(this, plainTextPswd);

            string sql =
                @"INSERT INTO customers (first, last, email, phone, adrL1, adrL2, city, state, zip, hashedPswd, insWhen, updWhen) " +
                "output INSERTED.customerId " +
                "VALUES (@first, @last, @email, @phone, @adrL1, @adrL2, @city, @state, @zip, @hashedPswd, @insWhen, @updWhen)";

            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            sqlCmd.Parameters.Add("@first", SqlDbType.VarChar).Value = First;
            sqlCmd.Parameters.Add("@last", SqlDbType.VarChar).Value = Last;
            sqlCmd.Parameters.Add("@email", SqlDbType.VarChar).Value = Email.ToLower(new CultureInfo("en-US", false));
            sqlCmd.Parameters.Add("@phone", SqlDbType.VarChar).Value = Phone;
            sqlCmd.Parameters.Add("@adrL1", SqlDbType.VarChar).Value = AdrL1;
            sqlCmd.Parameters.Add("@adrL2", SqlDbType.VarChar).Value = AdrL2;
            sqlCmd.Parameters.Add("@city", SqlDbType.VarChar).Value = City;
            sqlCmd.Parameters.Add("@state", SqlDbType.VarChar).Value = State.ToUpper(new CultureInfo("en-US", false));
            sqlCmd.Parameters.Add("@zip", SqlDbType.VarChar).Value = Zip;
            sqlCmd.Parameters.Add("@hashedPswd", SqlDbType.VarChar).Value = HashedPswd;
            sqlCmd.Parameters.Add("@insWhen", SqlDbType.DateTime).Value = DateTime.Now;
            sqlCmd.Parameters.Add("@updWhen", SqlDbType.DateTime).Value = DBNull.Value;

            try {
                CustomerId = (int)sqlCmd.ExecuteScalar(); // insert customer and return customer id
                return true;
            } catch (SqlException sqlEx) {
                Console.WriteLine(sqlEx.ToString());
                // check for error inserting duplicate emails
                if (sqlEx.Number == 2601 && sqlEx.Message.Contains("email"))
                    Error = "An account already exists with the email you entered";
                else Error = "We were unable to create your account at this time, please try again later";
                return false;
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                Error = "We were unable to create your account at this time, please try again later";
                return false;
            } finally {
                UseSql.Close(conn, sqlCmd);
            }
        }

        private bool ValidateUpdate() {
            string errorInit = "Please make the following changes:\r\n";
            string error = errorInit;
            string[] states = { "AK", "AL", "AR", "AS", "AZ", "CA", "CO", "CT", "DC", "DE", "FL", "GA", "GU", "HI", "IA", "ID", "IL", "IN", "KS", "KY", "LA", "MA", "MD", "ME", "MI", "MN", "MO", "MP", "MS", "MT", "NC", "ND", "NE", "NH", "NJ", "NM", "NV", "NY", "OH", "OK", "OR", "PA", "PR", "RI", "SC", "SD", "TN", "TX", "UM", "UT", "VA", "VI", "VT", "WA", "WI", "WV", "WY" };

            if (string.IsNullOrWhiteSpace(First))
                error += "• Enter your first name\r\n";
            if (string.IsNullOrWhiteSpace(Last))
                error += "• Enter your last name\r\n";
            if (string.IsNullOrWhiteSpace(AdrL1))
                error += "• Enter a street address\r\n";
            if (string.IsNullOrWhiteSpace(City))
                error += "• Enter a city\r\n";
            if (string.IsNullOrWhiteSpace(State))
                error += "• Enter a state abbreviation\r\n";
            else if (!states.Contains(State, StringComparer.OrdinalIgnoreCase))
                error += "• Enter a valid state abbreviation\r\n";
            if (string.IsNullOrWhiteSpace(Zip) || Zip.Length != 5 || !Zip.All(c => c >= '0' && c <= '9'))
                error += "• Enter a valid 5-digit zip code\r\n";
            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@') || !Email.Contains('.'))
                error += "• Enter a valid email address\r\n";

            if (error != errorInit) {
                InputErrors = error.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                return false;
            } else {
                return true;
            }
        }

        private bool ValidateUpdatePassword(string newPswd, string repeatNewPswd) {
            string errorInit = "Please make the following changes:\r\n";
            string error = errorInit;

            error = ValidateNewPassword(error, newPswd, repeatNewPswd);

            if (error != errorInit) {
                InputErrors = error.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                return false;
            } else {
                return true;
            }
        }

        public bool Update(int id) {
            if (!ValidateUpdate())
                return false;

            string sql = @"UPDATE customers SET first=@first, last=@last, email=@email, phone=@phone, adrL1=@adrL1, adrL2=@adrL2, " +
                "city=@city, state=@state, zip=@zip, hashedPswd=@hashedPswd, updWhen=@upd WHERE customerId=@customerId";

            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            sqlCmd.Parameters.Add("@customerId", SqlDbType.Int).Value = id;
            sqlCmd.Parameters.Add("@first", SqlDbType.VarChar).Value = First;
            sqlCmd.Parameters.Add("@last", SqlDbType.VarChar).Value = Last;
            sqlCmd.Parameters.Add("@email", SqlDbType.VarChar).Value = Email.ToLower(new CultureInfo("en-US", false));
            sqlCmd.Parameters.Add("@phone", SqlDbType.VarChar).Value = Phone;
            sqlCmd.Parameters.Add("@adrL1", SqlDbType.VarChar).Value = AdrL1;
            sqlCmd.Parameters.Add("@adrL2", SqlDbType.VarChar).Value = AdrL2;
            sqlCmd.Parameters.Add("@city", SqlDbType.VarChar).Value = City;
            sqlCmd.Parameters.Add("@state", SqlDbType.VarChar).Value = State.ToUpper(new CultureInfo("en-US", false));
            sqlCmd.Parameters.Add("@zip", SqlDbType.VarChar).Value = Zip;
            sqlCmd.Parameters.Add("@hashedPswd", SqlDbType.VarChar).Value = GetById(id).HashedPswd;
            sqlCmd.Parameters.Add("@upd", SqlDbType.DateTime).Value = DateTime.Now;

            try {
                int affected = sqlCmd.ExecuteNonQuery();
                return true;
            } catch (SqlException sqlEx) {
                Console.WriteLine(sqlEx.ToString());
                // check for error inserting duplicate emails
                if (sqlEx.Number == 2601 && sqlEx.Message.Contains("email"))
                    Error = "An account already exists with the email you entered";
                else Error = "We were unable to update your account at this time, please try again later";
                return false;
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                Error = "We were unable to update your account at this time, please try again later";
                return false;
            } finally {
                UseSql.Close(conn, sqlCmd);
            }
        }

        public bool UpdatePassword(int id, string oldPswd, string newPswd, string repeatNewPswd) {
            if (!Authenticator.CustomerAuth(Email, oldPswd)) {
                Error = "The current password you entered is incorrect";
                return false;
            }

            if (!ValidateUpdatePassword(newPswd, repeatNewPswd))
                return false;

            string sql = @"UPDATE customers SET hashedPswd=@hashedPswd, updWhen=@upd WHERE customerId=@customerId";

            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            sqlCmd.Parameters.Add("@customerId", SqlDbType.Int).Value = id;
            sqlCmd.Parameters.Add("@hashedPswd", SqlDbType.VarChar).Value = HashPassword(this, newPswd);
            sqlCmd.Parameters.Add("@upd", SqlDbType.DateTime).Value = DateTime.Now;

            try {
                int affected = sqlCmd.ExecuteNonQuery();
                return true;
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                Error = "We were unable to update your account at this time, please try again later";
                return false;
            } finally {
                UseSql.Close(conn, sqlCmd);
            }
        }

        /// <summary>
        /// Excludes hashed password value
        /// </summary>
        /// <returns> json string </returns>
        public string Serialize() {
            Customer customer = this;
            customer.HashedPswd = "";
            return JsonSerializer.Serialize(customer);
        }

        /// <summary>
        /// Excludes hashed password value
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public Customer Deserialize(string json) {
            Customer customer = JsonSerializer.Deserialize<Customer>(json);
            if (customer != null) {
                customer.HashedPswd = "";
                return customer;
            }
            throw new AppException("json string resolved to null");
        }

    }
}
