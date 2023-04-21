using Microsoft.AspNetCore.Mvc;
using MyApp.App.Biz;
using MyApp.App.ErrorHandling;
using MyApp.Models;
using System.Text.Json;

namespace MyApp.Controllers
{
    public class HomeController : BaseController
    {
        [HttpGet, Route("/")]
        public ActionResult Index() {
            HomeViewModel model = new HomeViewModel();
            Order order = new Order();
            string orderJson;
            // if no order is in session 
            if (HttpContext.Session.GetString("order") == null) {
                // set an initialized order as json in session
                order = new Order();
                HttpContext.Session.SetString("order", order.Serialize());
            } else { // get the order from session
                orderJson = HttpContext.Session.GetString("order");
                order = order.Deserialize(orderJson);
            }
            // if customer is in session associate customer with order
            if (HttpContext.Session.GetString("customer") != null) {
                string customerJson = HttpContext.Session.GetString("customer");
                Customer customer = new Customer();
                customer = customer.Deserialize(customerJson);
                // if customer id is not set to a customer
                if (order.CustomerId == 0) {
                    // add customer to the order
                    orderJson = order.AddCustomerInfo(customer);
                    // add updated order to session
                    HttpContext.Session.SetString("order", orderJson);
                }
                model.ProdList = customer.GetCustomizedProductList();
            } else {
                // amt: the number of products per page
                //int amt = 12; // keep amt a multiple of 12 so that there are an even number of products per row
                //int pg = 1; // the current page
                model.ProdList = Product.GetActiveList();              
            }
            return View("Index", model);
        }

        [HttpGet, Route("/login")]
        public ActionResult Login() {
            return View("Login");
        }

        [HttpPost, Route("/login/ajax-sign-in")]
        public ActionResult SignInCustomer(Dictionary<string, string> data) {
            string email = "";
            string plainTextPswd = "";
            // set values from sign in form
            if (data["email"] != null)
                email = data["email"];
            if (data["plainTextPswd"] != null)
                plainTextPswd = data["plainTextPswd"];

            bool success = Authenticator.CustomerAuth(email, plainTextPswd);
            if (success) {
                Order order;
                string orderJson;
                // Add customer to Session
                Customer customer = Customer.GetByEmail(email);
                // if order is in session associate customer with order
                if (HttpContext.Session.GetString("order") != null) {
                    orderJson = HttpContext.Session.GetString("order");
                    order = new Order();
                    order = order.Deserialize(orderJson);
                } else {
                    order = new Order();
                }
                // add customer to the order
                orderJson = order.AddCustomerInfo(customer);
                // add updated order to session
                HttpContext.Session.SetString("order", orderJson);
                // add customer to session
                HttpContext.Session.SetString("customer", customer.Serialize());
                return Json(new { msg = "Sign in complete" });
            } else {
                return StatusCode(StatusCodes.Status422UnprocessableEntity, new { errMsg = "The email or password you entered was incorrect" });
            }
        }

        [HttpPost, Route("/login/ajax-create-account")]
        public ActionResult CreateAccount(Dictionary<string, string> data) {
            Customer customer = new Customer();
            Order order;
            string orderJson;
            string plainTextPswd = "";
            string repeatPlainPswd = "";
            // set values from create account form
            if (data["email"] != null)
                customer.Email = data["email"];
            if (data["plainTextPswd"] != null)
                plainTextPswd = data["plainTextPswd"];
            if (data["repeatPlainPswd"] != null)
                repeatPlainPswd = data["repeatPlainPswd"];
            // attempt to insert customer
            bool success = customer.Insert(plainTextPswd, repeatPlainPswd);
            if (success) {
                customer = Customer.GetByEmail(customer.Email);
                // if order is in session associate customer with order
                if (HttpContext.Session.GetString("order") != null) {
                    orderJson = HttpContext.Session.GetString("order");
                    order = new Order();
                    order = order.Deserialize(orderJson);
                } else {
                    order = new Order();
                }
                // add customer to the order
                orderJson = order.AddCustomerInfo(customer);
                // add updated order to session
                HttpContext.Session.SetString("order", orderJson);
                // add customer to session
                HttpContext.Session.SetString("customer", customer.Serialize());
                return Json(new { msg = "Account created" });
            } else {
                // send customer insert validation errors to view
                return StatusCode(StatusCodes.Status422UnprocessableEntity, new { errMsg = customer.Error, inputErrors = customer.InputErrors });
            }
        }

        [HttpGet, Route("/myaccount")]
        public ActionResult MyAccount() {
            HomeViewModel model = new HomeViewModel();
            // if there is a customer in session 
            if (HttpContext.Session.GetString("customer") != null) {
                string json = HttpContext.Session.GetString("customer");
                Customer customer = new Customer();
                customer = customer.Deserialize(json);
                customer.Orders = Order.GetList(customer.CustomerId, true);
                model.Json = customer.Serialize(); // add it to the view model
            }
            return View("MyAccount", model);
        }

        [HttpPost, Route("/myaccount/ajax-save")]
        public ActionResult UpdateCustomer(Dictionary<string, string> data) {
            Customer customer = new Customer();
            customer.CustomerId = int.Parse(data["CustomerId"]);

            if (data["First"] != null)
                customer.First = data["First"]; 
            if (data["Last"] != null)
                customer.Last = data["Last"];
            if (data["Email"] != null)
                customer.Email = data["Email"];
            if (data["Phone"] != null)
                customer.Phone = data["Phone"];
            if (data["AdrL1"] != null)
                customer.AdrL1 = data["AdrL1"];
            if (data["AdrL2"] != null)
                customer.AdrL2 = data["AdrL2"];
            if (data["City"] != null)
                customer.City = data["City"];
            if (data["State"] != null)
                customer.State = data["State"];
            if (data["Zip"] != null)
                customer.Zip = data["Zip"];

            string oldPswd = "";
            string newPswd = "";
            string repeatNewPswd = "";

            if (data["oldPswd"] != null)
                oldPswd = data["oldPswd"];
            if (data["newPswd"] != null)
                newPswd = data["newPswd"];
            if (data["repeatNewPswd"] != null)
                repeatNewPswd = data["repeatNewPswd"];

            bool success;
            // if a password variable was set
            if (oldPswd != "" || newPswd != "" || repeatNewPswd != "") {
                success = customer.UpdatePassword(customer.CustomerId, oldPswd, newPswd, repeatNewPswd);
            } else {
                success = customer.Update(customer.CustomerId);
            }
            if (!success) {
                return StatusCode(StatusCodes.Status422UnprocessableEntity, new { inputErrors = customer.InputErrors, errMsg = customer.Error });
            }
            string orderJson;
            Order order;
            // if order is in session associate customer with order
            if (HttpContext.Session.GetString("order") != null) {
                orderJson = HttpContext.Session.GetString("order");
                order = new Order();
                order = order.Deserialize(orderJson);
            } else {
                order = new Order();
            }
            // add customer to the order
            orderJson = order.AddCustomerInfo(customer);
            // add updated order to session
            HttpContext.Session.SetString("order", orderJson);
            // add customer to session
            HttpContext.Session.SetString("customer", customer.Serialize());
            return Json(new { msg = "Account updated successfully" });
        }
        [HttpPost, Route("/myaccount/ajax-customer")]
        public ActionResult GetCustomerInfo() {
            string json = HttpContext.Session.GetString("customer");
            Customer customer = JsonSerializer.Deserialize<Customer>(json);
            return Json(new { customer });
        }

        [HttpPost, Route("/myaccount/ajax-sign-out")]
        public ActionResult SignOutCustomer() {
            HttpContext.Session.Clear();
            return Json(new { msg = "Customer signed out" });
        }

    }
}
