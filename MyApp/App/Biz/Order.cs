﻿using Microsoft.Data.SqlClient;
using MyApp.App.Utils;
using System.Data;
using System.Text.Json;
using System.Runtime.Serialization;
using MyApp.App.ErrorHandling;
using MyApp.App.PaymentGateway;
using Braintree;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Globalization;
using System;

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
            InputErrors = new string[0];
        }

        public static Order GetById(int id, bool getLines) {
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

                if (getLines)
                    order.Lines = OrderLine.GetList(order.OrderId);

                return order;

            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());

                return new Order() { Error = "Unable to find order in the database", OrderId = id };

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
            // ensure order total is correct by using database product prices 
            CalcTotals();

            // validate form input
            if (ValidFormEntries() == false) {
                return false;
            }

            // facilitate PayPal transaction via the Braintree server SDK
            bool transactionSuccess = CreateTransaction();
            if(transactionSuccess == false) {
                return false;
            }

            // insert into database if successful

            string sql =
                @"INSERT INTO orders (customerId, first, last, email, phone, adrL1, adrL2, city, state, zip, subtotal, shipping, total, orderDate, transactionId, paymentMethodNonce) " +
                "output INSERTED.orderId " +
                "VALUES (@customerId, @first, @last, @email, @phone, @adrL1, @adrL2, @city, @state, @zip, @subtotal, @shipping, @total, @orderDate, @transactionId, @paymentMethodNonce)";

            (SqlConnection conn, SqlCommand sqlCmd, SqlTransaction transaction) = UseSql.SetUpTransaction(sql);
            sqlCmd.Parameters.Add("@customerId", SqlDbType.Int).Value = CustomerId;
            sqlCmd.Parameters.Add("@first", SqlDbType.VarChar).Value = First;
            sqlCmd.Parameters.Add("@last", SqlDbType.VarChar).Value = Last;
            sqlCmd.Parameters.Add("@email", SqlDbType.VarChar).Value = Email.ToLower(new CultureInfo("en-US", false));
            sqlCmd.Parameters.Add("@phone", SqlDbType.VarChar).Value = Phone;
            sqlCmd.Parameters.Add("@adrL1", SqlDbType.VarChar).Value = AdrL1;
            sqlCmd.Parameters.Add("@adrL2", SqlDbType.VarChar).Value = AdrL2;
            sqlCmd.Parameters.Add("@city", SqlDbType.VarChar).Value = City;
            sqlCmd.Parameters.Add("@state", SqlDbType.VarChar).Value = State.ToUpper(new CultureInfo("en-US", false));
            sqlCmd.Parameters.Add("@zip", SqlDbType.VarChar).Value = Zip;
            sqlCmd.Parameters.Add("@subtotal", SqlDbType.Money).Value = Subtotal;
            sqlCmd.Parameters.Add("@shipping", SqlDbType.Money).Value = Shipping;
            sqlCmd.Parameters.Add("@total", SqlDbType.Money).Value = Total;
            sqlCmd.Parameters.Add("@orderDate", SqlDbType.DateTime).Value = DateTime.Now;
            sqlCmd.Parameters.Add("@transactionId", SqlDbType.VarChar).Value = TransactionId;
            sqlCmd.Parameters.Add("@paymentMethodNonce", SqlDbType.VarChar).Value = paymentMethodNonce;

            try {
                // insert order and return order id
                OrderId = (int)sqlCmd.ExecuteScalar(); 

                // insert order lines
                foreach (OrderLine line in Lines) {
                    line.OrderId = OrderId;
                    bool success = line.Insert(conn, transaction);

                    if (success == false) { 
                        // an order line failed to be inserted after the order was inserted
                        Error = "Unable to insert an order line";
                        transaction.Rollback();

                        return false; // todo: atleast log the order line which failed
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
        public static List<Order> GetList() {
            string sql = @"SELECT orderId, customerId, first, last, email, phone, adrL1, adrL2, city, state, zip, subtotal, shipping, total, orderDate, transactionId FROM orders";
            
            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            SqlDataReader reader = null;
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
                    orders.Add(order);
                }

                return orders;

            } finally {
                UseSql.Close(conn, sqlCmd, reader);
            }
        }

        public static List<Order> GetList(int customerId, bool getLines) {
            string sql = @"SELECT orderId, customerId, first, last, email, phone, adrL1, adrL2, city, state, zip, subtotal, shipping, total, orderDate, transactionId " +
                "FROM orders WHERE customerId=@customerId";

            (SqlConnection conn, SqlCommand sqlCmd) = UseSql.ConnAndCmd(sql);
            sqlCmd.Parameters.Add("@customerId", SqlDbType.Int).Value = customerId;
            SqlDataReader reader = null;
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

                    if (getLines) {
                        order.Lines = OrderLine.GetList(order.OrderId); // probably slower than one call to orderlines with a where clause and sorting/pairing orderlines to each order
                    }  
                    
                    orders.Add(order);
                }
                return orders;

            } finally {
                UseSql.Close(conn, sqlCmd, reader);
            }
        }

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

            OrderLine line = Lines.Find(o => o.ProductId == prod.ProductId);

            if (line != null) {
                line.Qty++;

            } else {
                line = new OrderLine();
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

        public string QtyPlus(int productId) {
            OrderLine line = Lines.Find(ol => ol.ProductId == productId);

            if (line != null)
                line.Qty++;

            return Serialize();
        }

        public string QtyMinus(int productId) {
            OrderLine line = Lines.Find(ol => ol.ProductId == productId);

            if (line != null) {
                line.Qty--;
                if (line.Qty == 0) {
                    Lines.Remove(line);
                }
            }

            return Serialize();
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
        public string Serialize() {
            CalcTotals();
            return JsonSerializer.Serialize(this);
        }

        /// <summary>
        /// Recalulcates totals before deserializing
        /// </summary>
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
        /// Set the Transaction property via the TransactionId and the Braintree server SDK
        /// <br/> - used to set the Transaction property after retrieving a TransactionId from the db
        /// </summary>
        public void SetTransaction() {
            IBraintreeGateway gateway = braintreeConfig.GetGateway();
            Transaction = gateway.Transaction.Find(TransactionId);
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
                // success
                Transaction = result.Target;
                TransactionId = Transaction.Id;
                return true;
            } else if (result.Transaction != null) {
                // soft decline // pending status

                // Category 1: Do not retry
                //      [2044, 2047, 2019, 2007, 2009, 2108, 2015, 2017, 2018]
                // Category 2: Issuer Cannot Approve At this Time (Reattempt Allowed); limit retries to 15 retries over a rolling 30 day period
                //      [2026, 2038, 2001, 2014, 2002, 2057, 2003, 2101, 2107, 2038*, 2046, 2020, 3000]
                // Category 3: Limit retries to 15 retries over a rolling 30 day period
                //      [2004, 2007, 2102, 2010, 2101, 2103, 2038*]
                // Category 4: Limit retries to 15 retries over a rolling 30 day period
                //      [2000]

                // 2000 - Do Not Honor -
                //      The customer's bank is unwilling to accept the transaction.

                // 2001 - Insufficient Funds -
                //      The account did not have sufficient funds to cover the transaction amount at the time of the transaction

                // 2002 - Limit Exceeded -
                //      The attempted transaction exceeds the withdrawal limit of the account.

                // 2003 - Cardholder's Activity Limit Exceeded -
                //      The attempted transaction exceeds the activity limit of the account.

                // 2016 - Duplicate Transaction
                //      The submitted transaction appears to be a duplicate of a previously submitted transaction and was declined
                //      to prevent charging the same card twice for the same service.

                // 2025 -> 2030 - Set Up Error


                // other

                // hard decline

                // 2004 - Expired Card -
                // Do not retry the transaction

                // 2005 - Invalid Credit Card Number - 
                // Do not retry the transaction

                // 2006 - Invalid Expiraion Date -

                // 2007 - No Account -
                //      The submitted card number is not on file with the card-issuing bank.

                // 2008 - Card Account Length Error - 
                //      The submitted card number does not include the proper number of digits.

                // 2009 - No Such Issuer -
                //      This decline code could indicate that the submitted card number does not correlate to an existing
                //      card-issuing bank or that there is a connectivity error with the issuer.

                // 2010 - Card Issuer Declined CVV -
                //      The customer entered in an invalid security code or made a typo in their card information.


                TransactionId = result.Transaction.Id;
                return true;
            } else {
                // There was a problem processing your credit card; please double check your payment information and try again.
                TransactionErrors = "";
                foreach (ValidationError error in result.Errors.DeepAll()) {
                    TransactionErrors += "Error: " + (int)error.Code + " - " + error.Message + "\n";
                } 
                return false;  
            }
        }

    }
}
