using Microsoft.Data.SqlClient;
using MyApp.App.Utils;
using System.Data;
using System.Text.Json;
using System.Runtime.Serialization;
using MyApp.App.ErrorHandling;
using MyApp.App.PaymentGateway;
using Braintree;
using System.Reflection;
using System.Text.Json.Serialization;

namespace MyApp.App.Biz
{
    public class Order
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
        public string AdrL1 { get; set; }
        public string AdrL2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public decimal Subtotal { get; private set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; private set; }
        public DateTime OrderDate { get; set; }
        public string TransactionId { get; private set; }
        [JsonIgnore]
        private string paymentMethodNonce;

        public List<OrderLine> Lines { get; set; }
        public string Error { get; private set; }
        public string[] InputErrors { get; private set; }
        private const int requiredPlusHeader = 7 + 1; // defines InputErrors size
        // Braintree
        public string TransactionErrors;
        public Transaction Transaction { get; private set; }

        public Order() {
            OrderId = 0;
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
            Subtotal = 0;
            Shipping = 0;
            Total = 0;
            OrderDate = DateTime.MinValue;
            Lines = new List<OrderLine>();
            Error = "";
            InputErrors = new string[requiredPlusHeader];
            IgnoreDataMemberAttribute nonce = new IgnoreDataMemberAttribute();
        }

        public Order GetById(int id, bool getLines) {
            string sql = @"SELECT customerId, first, last, email, phone, adrL1, adrL2, city, state, zip, subtotal, shipping, total, orderDate, transactionId, paymentMethodNonce " +
                "FROM orders WHERE orderId = @orderId";

            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            sqlCmd.Parameters.Add("@orderId", SqlDbType.Int).Value = id;

            SqlDataReader reader = sqlCmd.ExecuteReader();
            Order order = new Order();
            order.OrderId = id;
            int index = 0;
            try {
                reader.Read();
                order.CustomerId = reader.GetInt32(index++);
                order.First = reader.GetString(index++);
                order.Last = reader.GetString(index++);
                order.Email = reader.GetString(index++);
                order.Phone = reader.GetString(index++);
                order.AdrL1 = reader.GetString(index++);
                order.AdrL2 = reader.GetString(index++);
                order.City = reader.GetString(index++);
                order.State = reader.GetString(index++);
                order.Zip = reader.GetString(index++);
                order.Subtotal = reader.GetDecimal(index++);
                order.Shipping = reader.GetDecimal(index++);
                order.Total = reader.GetDecimal(index++);
                order.OrderDate = reader.GetDateTime(index++);
                order.TransactionId = reader.GetString(index++);
                order.paymentMethodNonce = reader.GetString(index++);
                reader.Close();
                if (getLines)
                    order.Lines = OrderLine.GetAllOrderLines(order.OrderId);
                return order;
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return new Order() { Error = "Unable to find order in the database", OrderId = id }; ;
            } finally {
                UseSql.Close(conn, sqlCmd, reader);
            }
        }

        private bool ValidFormEntries() {
            string errorInit = "Please make the following changes:\r\n";
            string error = errorInit;
            string[] states = { "AK", "AL", "AR", "AS", "AZ", "CA", "CO", "CT", "DC", "DE", "FL", "GA", "GU", "HI", "IA", "ID", "IL", "IN", "KS", "KY", "LA", "MA", "MD", "ME", "MI", "MN", "MO", "MP", "MS", "MT", "NC", "ND", "NE", "NH", "NJ", "NM", "NV", "NY", "OH", "OK", "OR", "PA", "PR", "RI", "SC", "SD", "TN", "TX", "UM", "UT", "VA", "VI", "VT", "WA", "WI", "WV", "WY" };

            if (string.IsNullOrWhiteSpace(First))
                error += "• Enter a first name for delivery\r\n";
            if (string.IsNullOrWhiteSpace(Last))
                error += "• Enter a last name for delivery\r\n";
            if (string.IsNullOrWhiteSpace(AdrL1))
                error += "• Enter the street address\r\n";
            if (string.IsNullOrWhiteSpace(City))
                error += "• Enter the city\r\n";
            if (string.IsNullOrWhiteSpace(State))
                error += "• Enter the state abbreviation\r\n";
            else if (!states.Contains(State, StringComparer.OrdinalIgnoreCase))
                error += "• Enter a valid state abbreviation\r\n";
            if (string.IsNullOrWhiteSpace(Zip) || Zip.Length != 5 || !Zip.All(c => c >= '0' && c <= '9'))
                error += "• Enter a valid 5-digit zip code\r\n";
            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@') || !Email.Contains('.'))
                error += "• Enter a valid email address\r\n";

            if (error != errorInit) {
                InputErrors = error.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                return false;
            }
            return true;
        }

        public bool Insert() {
            // validate the order total off of database product prices
            ValidateTotal(); // throws if the total is invalid

            // validate order info
            if (!ValidFormEntries())
                return false;

            // facilitate PayPal transaction via the Braintree server SDK
            bool transactionSuccess = CreateTransaction();
            if(!transactionSuccess) 
                return false;

            // insert into database if successful

            string sql =
                @"INSERT INTO orders (customerId, first, last, email, phone, adrL1, adrL2, city, state, zip, subtotal, shipping, total, orderDate, transactionId, paymentMethodNonce) " +
                "output INSERTED.orderId " +
                "VALUES (@customerId, @first, @last, @email, @phone, @adrL1, @adrL2, @city, @state, @zip, @subtotal, @shipping, @total, @orderDate, @transactionId, @paymentMethodNonce)";

            (SqlConnection conn, SqlCommand sqlCmd, SqlTransaction transaction) = UseSql.SetUpTransaction(sql);
            sqlCmd.Parameters.Add("@customerId", SqlDbType.Int).Value = CustomerId;
            sqlCmd.Parameters.Add("@first", SqlDbType.VarChar).Value = First;
            sqlCmd.Parameters.Add("@last", SqlDbType.VarChar).Value = Last;
            sqlCmd.Parameters.Add("@email", SqlDbType.VarChar).Value = Email;
            sqlCmd.Parameters.Add("@phone", SqlDbType.VarChar).Value = Phone;
            sqlCmd.Parameters.Add("@adrL1", SqlDbType.VarChar).Value = AdrL1;
            sqlCmd.Parameters.Add("@adrL2", SqlDbType.VarChar).Value = AdrL2;
            sqlCmd.Parameters.Add("@city", SqlDbType.VarChar).Value = City;
            sqlCmd.Parameters.Add("@state", SqlDbType.VarChar).Value = State;
            sqlCmd.Parameters.Add("@zip", SqlDbType.VarChar).Value = Zip;
            sqlCmd.Parameters.Add("@subtotal", SqlDbType.Money).Value = Subtotal;
            sqlCmd.Parameters.Add("@shipping", SqlDbType.Money).Value = Shipping;
            sqlCmd.Parameters.Add("@total", SqlDbType.Money).Value = Total;
            sqlCmd.Parameters.Add("@orderDate", SqlDbType.DateTime).Value = DateTime.Now;
            sqlCmd.Parameters.Add("@transactionId", SqlDbType.VarChar).Value = TransactionId;
            sqlCmd.Parameters.Add("@paymentMethodNonce", SqlDbType.VarChar).Value = paymentMethodNonce;

            try {
                OrderId = (int)sqlCmd.ExecuteScalar(); // insert order and return order id
                // insert order lines
                foreach (OrderLine line in Lines) {
                    line.OrderId = OrderId;
                    bool success = line.Insert(conn, transaction);
                    if (!success) { // should you delete a partially inserted order when an orderLine fails to be inserted?
                        Error = "Unable to insert an order line";
                        return false; // atleast log the order line which failed
                    }
                }
                transaction.Commit();
                return true;
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                Error = "Unable to insert order into the database";
                transaction.Rollback();
                return false;
            } finally {
                UseSql.Close(conn, sqlCmd);
            }
        }

        /// <summary>
        /// Does not get paymentMethodNonce
        /// </summary>
        public static List<Order> GetAllOrders(bool getLines) {
            string sql = @"SELECT orderId, customerId, first, last, email, phone, adrL1, adrL2, city, state, zip, subtotal, shipping, total, orderDate, transactionId FROM orders";
            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            SqlDataReader? reader = null;
            List<Order> orders = new List<Order>();
            try {
                reader = sqlCmd.ExecuteReader();
                while (reader.Read()) {
                    Order order = new Order();
                    int index = 0;
                    order.OrderId = reader.GetInt32(index++);
                    order.CustomerId = reader.GetInt32(index++);
                    order.First = reader.GetString(index++);
                    order.Last = reader.GetString(index++);
                    order.Email = reader.GetString(index++);
                    order.Phone = reader.GetString(index++);
                    order.AdrL1 = reader.GetString(index++);
                    order.AdrL2 = reader.GetString(index++);
                    order.City = reader.GetString(index++);
                    order.State = reader.GetString(index++);
                    order.Zip = reader.GetString(index++);
                    order.Subtotal = reader.GetDecimal(index++);
                    order.Shipping = reader.GetDecimal(index++);
                    order.Total = reader.GetDecimal(index++);
                    order.OrderDate = reader.GetDateTime(index++);
                    order.TransactionId = reader.GetString(index++);
                    if (getLines)
                        order.Lines = OrderLine.GetAllOrderLines(order.OrderId);
                    orders.Add(order);
                }
                return orders;
            } finally {
                if (reader != null) reader.Close();
                UseSql.Close(conn, sqlCmd);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="getLines"></param>
        /// <returns></returns>
        public static List<Order> GetAllOrders(int customerId, bool getLines) {
            string sql = @"SELECT orderId, customerId, first, last, email, phone, adrL1, adrL2, city, state, zip, subtotal, shipping, total, orderDate, transactionId " +
                "FROM orders WHERE customerId=@customerId";
            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            sqlCmd.Parameters.Add("@customerId", SqlDbType.Int).Value = customerId;
            SqlDataReader? reader = null;
            List<Order> orders = new List<Order>();
            try {
                reader = sqlCmd.ExecuteReader();
                while (reader.Read()) {
                    Order order = new Order();
                    int index = 0;
                    order.OrderId = reader.GetInt32(index++);
                    order.CustomerId = reader.GetInt32(index++);
                    order.First = reader.GetString(index++);
                    order.Last = reader.GetString(index++);
                    order.Email = reader.GetString(index++);
                    order.Phone = reader.GetString(index++);
                    order.AdrL1 = reader.GetString(index++);
                    order.AdrL2 = reader.GetString(index++);
                    order.City = reader.GetString(index++);
                    order.State = reader.GetString(index++);
                    order.Zip = reader.GetString(index++);
                    order.Subtotal = reader.GetDecimal(index++);
                    order.Shipping = reader.GetDecimal(index++);
                    order.Total = reader.GetDecimal(index++);
                    order.OrderDate = reader.GetDateTime(index++);
                    order.TransactionId = reader.GetString(index++);
                    if (getLines)
                        order.Lines = OrderLine.GetAllOrderLines(order.OrderId);
                    orders.Add(order);
                }
                return orders;
            } finally {
                if (reader != null) reader.Close();
                UseSql.Close(conn, sqlCmd);
            }
        }

        //public static List<Order> GetSomeOrders(int amt, int pg) {
        //    List<Order> list = new List<Order>();
        //    Order order = new Order();
        //    int id = 1000; //db starts at 1000
        //    int offset = (pg - 1) * amt;
        //    amt += 1000;
        //    for (id += offset; id < amt; id++) {
        //        order = order.GetById(id);
        //        list.Add(order);
        //    }
        //    return list;
        //}

        public string AddCustomerInfo(Customer customer) {
            // fill in corresponding fields
            CustomerId = customer.CustomerId;
            First = customer.First;
            Last = customer.Last;
            AdrL1 = customer.AdrL1;
            AdrL2 = customer.AdrL2;
            City = customer.City;
            State = customer.State;
            Zip = customer.Zip;
            Email = customer.Email;
            Phone = customer.Phone;
            return Serialize();
        }

        public string AddToCart(int productId, int customerId) {
            Product prod = new Product();
            prod = prod.GetById(productId);
            if (Lines.Find(ol => ol.ProductId == productId) != null) {
                OrderLine line = Lines.Find(o => o.ProductId == prod.ProductId);
                line.Qty++;
            } else {
                OrderLine line = new OrderLine();
                line.CustomerId = customerId;
                line.ProductId = productId;
                line.Name = prod.Name;
                line.UnitPrice = prod.Price;
                line.Descr = prod.Descr;
                line.Qty = 1;
                line.ImgUrl = prod.ImgUrl; 
                Lines.Add(line);
            }
            return Serialize();
        }

        public string RmvLine(int productId) {
            Product prod = new Product();
            prod = prod.GetById(productId);
            if (Lines.Find(ol => ol.ProductId == productId) != null) {
                OrderLine line = Lines.Find(ol => ol.ProductId == productId);
                Lines.Remove(line);
            }
            return Serialize();
        }

        public string QtyPlus(int productId) {
            Product prod = new Product();
            prod = prod.GetById(productId);
            if (Lines.Find(ol => ol.ProductId == productId) != null) {
                OrderLine line = Lines.Find(ol => ol.ProductId == productId);
                line.Qty++;
            }
            return Serialize();
        }

        public string QtyMinus(int productId) {
            Product prod = new Product();
            prod = prod.GetById(productId);
            if (Lines.Find(ol => ol.ProductId == productId) != null) {
                OrderLine line = Lines.Find(ol => ol.ProductId == productId);
                line.Qty--;
                if (line.Qty == 0)
                    Lines.Remove(line);
            }
            return Serialize();
        }

        private void ValidateTotal() {
            decimal subtotal = 0;
            if (Lines.Count != 0) {
                foreach (OrderLine line in Lines) {
                    Product prod = new Product();
                    prod = prod.GetById(line.ProductId);
                    subtotal += (prod.Price * line.Qty);
                }
            }
            decimal total = subtotal + Shipping;
            if (Total != total)
                throw new AppException("Order Total did not pass validation");
        }

        private void CalcTotals() {
            Subtotal = 0;
            if (Lines.Count != 0) {
                foreach (OrderLine line in Lines) {
                    Subtotal += (line.UnitPrice * line.Qty);
                }
            }
            Total = Subtotal + Shipping;
        }

        /// <summary>
        /// Calculates totals before serializing to json
        /// </summary>
        /// <returns> json string </returns>
        public string Serialize() {
            CalcTotals();
            return JsonSerializer.Serialize(this);
        }

        /// <summary>
        /// Recalulcates totals from Db
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public Order Deserialize(string json) {
            Order order = JsonSerializer.Deserialize<Order>(json);
            if (order != null) {
                order.CalcTotals();
                return order;
            }
            throw new AppException("json string resolved to null");
        }


        //                                                      //
        //  Braintree secure payment gateway integration below  //
        //                                                      //
        private BraintreeConfiguration braintreeConfig = new BraintreeConfiguration();
        private static readonly TransactionStatus[] transactionSuccessStatuses = {
                                                                                    TransactionStatus.AUTHORIZED,
                                                                                    TransactionStatus.AUTHORIZING,
                                                                                    TransactionStatus.SETTLED,
                                                                                    TransactionStatus.SETTLING,
                                                                                    TransactionStatus.SETTLEMENT_CONFIRMED,
                                                                                    TransactionStatus.SETTLEMENT_PENDING,
                                                                                    TransactionStatus.SUBMITTED_FOR_SETTLEMENT
                                                                                };
        /// <summary>
        /// generate client token for braintree dropin UI authentication and creation
        /// </summary>
        public string GetClientToken() {
            // get secure payment gateway from the braintree configuration
            IBraintreeGateway gateway = braintreeConfig.GetGateway();
            return gateway.ClientToken.Generate();
        }

        public void SetPaymentMethodNonce(string nonce) {
            paymentMethodNonce = nonce;
        }

        /// <summary>
        /// Create a Transaction using the Braintree server SDK 
        /// <br/> - Sets transactionId and Transaction on success 
        /// <br/> - Sets transactionId on !null
        /// <br/> - Sets TransactionErrors otherwise
        /// </summary>
        /// 
        private bool CreateTransaction() {
            // get secure payment gateway from the braintree configuration
            IBraintreeGateway gateway = braintreeConfig.GetGateway();

            TransactionRequest request = new TransactionRequest() {
                Amount = Total,
                PaymentMethodNonce = paymentMethodNonce,
                Options = new TransactionOptionsRequest {
                    SubmitForSettlement = true
                }
            };

            Result<Transaction> result = gateway.Transaction.Sale(request);
            if (result.IsSuccess()) {
                Transaction = result.Target;
                TransactionId = Transaction.Id;
                return true;
            } else if (result.Transaction != null) {
                TransactionId = result.Transaction.Id;
                return true;
            } else {
                TransactionErrors = "";
                foreach (ValidationError error in result.Errors.DeepAll()) {
                    TransactionErrors += "Error: " + (int)error.Code + " - " + error.Message + "\n";
                }
                return false;
            }
        }

        public bool CreditCardVerification() {
            return true;
        }

        /// <summary>
        /// Set the Transaction property via the TransactionId and the Braintree server SDK
        /// <br/> - used to set the Transaction property after retrieving a TransactionId from the db
        /// </summary>
        public void SetTransaction() {
            IBraintreeGateway gateway = braintreeConfig.GetGateway();
            Transaction = gateway.Transaction.Find(TransactionId);
        }


        // proabably can delete this as I will not add an unsuccessful transaction into the database
        /// <summary>
        /// Determines whether or not a Transaction was successful using transactionSuccessStatuses
        /// <br/> - handle 
        /// </summary>
        private bool TransactionSuccess() {
            IBraintreeGateway gateway = braintreeConfig.GetGateway();

            Transaction = gateway.Transaction.Find(TransactionId);
            if (transactionSuccessStatuses.Contains(Transaction.Status)) {
                return true;
            } else {

                return false;
            }

        }


    }
}
