using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using MyApp.App.Biz;
using MyApp.Models;
using MyApp.App.ErrorHandling;
using MyApp.App.PaymentGateway;
using MyApp.App.Utils;

namespace MyApp.Controllers
{
    public class OrderController : BaseController
    {
        /* Cart Section */

        [HttpGet, Route("/cart")]
        public ActionResult Cart() {
            OrderViewModel model = new OrderViewModel();

            string jsonOrder = HttpContext.Session.GetString("order");
            if (jsonOrder != null) { 
                model.JsonOrder = jsonOrder;
            } else {
                // add a new order to session
                Order order = new Order();
                HttpContext.Session.SetString("order", order.Serialize());
                model.JsonOrder = order.Serialize();
            }

            return View("Cart", model);
        }

        [HttpPost, Route("/cart/ajax-add")]
        public ActionResult AddToCart(Dictionary<string, string> data) {
            Order order = new Order();

            string jsonOrder = HttpContext.Session.GetString("order");
            if (jsonOrder != null) {
                order = order.Deserialize(jsonOrder);
            } else {
                throw new AppException("Error processing order string from session");
            }

            // get customerId from session
            int customerId = 0;
            string jsonCustomer = HttpContext.Session.GetString("customer");

            if (jsonCustomer != null) {
                Customer customer = new Customer();
                customer = customer.Deserialize(jsonCustomer);
                customerId = customer.CustomerId;
            }

            int productId = int.Parse(data["productId"]);
            jsonOrder = order.AddToCart(productId, customerId);
            HttpContext.Session.SetString("order", jsonOrder);

            return Json(new { msg = "Product added successfully" });
        }

        [HttpPost, Route("/cart/ajax-qty-plus")]
        public ActionResult QtyPlus(Dictionary<string, string> data) {
            Order order = new Order();
            string jsonOrder = HttpContext.Session.GetString("order");

            // get order from session
            if (jsonOrder != null) {
                order = order.Deserialize(jsonOrder);
            } else {
                throw new AppException("Error processing order string from session");
            }

            int productId = int.Parse(data["productId"]);
            jsonOrder = order.QtyPlus(productId);
            HttpContext.Session.SetString("order", jsonOrder);

            return Json(jsonOrder);
        }

        [HttpPost, Route("/cart/ajax-qty-minus")]
        public ActionResult QtyMinus(Dictionary<string, string> data) {
            Order order = new Order();
            string json = HttpContext.Session.GetString("order");

            // get order from session
            if (json != null) {
                order = order.Deserialize(json);
            } else {
                throw new AppException("Error processing order string from session");
            }

            int productId = int.Parse(data["productId"]);
            json = order.QtyMinus(productId);
            HttpContext.Session.SetString("order", json);

            return Json(json);
        }

        /* Checkout Section */

        [HttpGet, Route("/checkout")]
        public ActionResult Checkout() {
            OrderViewModel model = new OrderViewModel();
            Order order = new Order();
            string jsonOrder = HttpContext.Session.GetString("order");

            if (jsonOrder != null) { // an order is in session
                model.JsonOrder = jsonOrder;
                // get client token for braintree dropin UI authentication / creation
                model.ClientToken = order.GetClientToken();

            } else {
                HttpContext.Session.SetString("order", order.Serialize());
                model.JsonOrder = order.Serialize();
            }

            return View("Checkout", model);
        }

        [HttpPost, Route("/checkout/ajax-ins-order")]
        public ActionResult InsertOrder(Dictionary<string, string> data) {
            string json = HttpContext.Session.GetString("order");
            Order order = new Order();
            order = order.Deserialize(json);

            // set order values from checkout form
            if (data["First"] != null)
                order.First = data["First"];
            if (data["Last"] != null)
                order.Last = data["Last"];
            if (data["Email"] != null)
                order.Email = data["Email"];
            if (data["Phone"] != null)
                order.Phone = data["Phone"];
            if (data["AdrL1"] != null)
                order.AdrL1 = data["AdrL1"];
            if (data["AdrL2"] != null)
                order.AdrL2 = data["AdrL2"];
            if (data["City"] != null)
                order.City = data["City"];
            if (data["State"] != null)
                order.State = data["State"];
            if (data["Zip"] != null)
                order.Zip = data["Zip"];
            if (data["expressShip"] != null) {
                bool express = bool.Parse(data["expressShip"]);
                if (express) order.Shipping = decimal.Parse(AppSettings.GetString("ExpressShippingCost"));
            }

            // set payment method nonce
            if (data["paymentMethodNonce"] != null)
                order.SetPaymentMethodNonce(data["paymentMethodNonce"]);          

            bool success = order.Insert();
            if (success) {
                int orderId = order.OrderId;
                // set orderId in session - this only happens once an order has been completed
                HttpContext.Session.SetInt32("orderId", orderId);

                // reset order in session to a new order
                order = new Order();
                HttpContext.Session.SetString("order", order.Serialize());

                return Json(new { msg = "Order Completed" });

            } else {
                // improve: do something with Braintree transaction errors // order.TransactionErrors

                HttpContext.Session.SetString("order", order.Serialize()); // save form input values and input errors in session

                return StatusCode(StatusCodes.Status422UnprocessableEntity, new { inputErrors = order.InputErrors, path = "/checkout/ajax-ins-order" });
            }
        }

        [HttpGet, Route("/checkout/thank-you")]
        public ActionResult ThankYou() {
            OrderViewModel model = new OrderViewModel();

            // if there is an orderId in session 
            if (HttpContext.Session.GetInt32("orderId") != null) { // only in session after placing an order
                int orderId = (int)HttpContext.Session.GetInt32("orderId");

                // add order info and BrainTree transaction info to the model
                Order order = new Order();
                order = Order.GetById(orderId, true);
                order.SetTransaction();
                model.JsonOrder = order.Serialize();
            }

            return View("ThankYou", model); 
        }


    }
}