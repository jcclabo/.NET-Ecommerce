using Microsoft.AspNetCore.Mvc;
using MyApp.App.Biz;
using MyApp.Models;
using System.Text.Json;

namespace MyApp.Controllers
{
    public class AdminController : Controller
    {
        [HttpGet, Route("/admin")]
        public ActionResult AdminDashboard() {
            return View("AdminDashboard");
        }

        [HttpGet, Route("/admin/login")]
        public ActionResult AdminLogin() {
            return View("AdminLogin");
        }

        [HttpPost, Route("/admin/login/ajax-sign-in")]
        public ActionResult AdminSignIn(Dictionary<string, string> data) {
            string email = "";
            string plainTextPswd = "";
            // set values from sign in form
            if (data["email"] != null)
                email = data["email"];
            if (data["plainTextPswd"] != null)
                plainTextPswd = data["plainTextPswd"];

            if (HttpContext.Session.GetString("admin") == null) {
                bool success = Authenticator.AdminAuth(email, plainTextPswd);
                if (success) {

                    ViewBag.Admin = true;

                    // add admin to session
                    HttpContext.Session.SetString("admin", "true");
                    return Json(new { msg = "Sign in complete" });
                } else {
                    return StatusCode(StatusCodes.Status422UnprocessableEntity, new { errMsg = "The email or password you entered was incorrect" });
                }
            } else {
                return Json(new { msg = "Sign in complete" });
            }
        }

        [HttpGet, Route("/admin/product")]
        public ActionResult AdminProduct(int? productId) {
            AdminViewModel model = new AdminViewModel();
            if (productId != null) {
                int id = (int)productId;
                Product prod = new Product();
                prod = prod.GetById(id);
                model.Json = prod.Serialize();
            }
            return View("AdminProduct", model);     
        }

        [HttpPost, Route("/admin/product/ajax-find")]
        public ActionResult FindProduct(Dictionary<string, string> data) {
            Product prod = new Product();
            if (data["productId"] != null)
                prod.ProductId = int.Parse(data["productId"]);
            else return StatusCode(StatusCodes.Status422UnprocessableEntity, new { errMsg = "Please specify the Product ID" });

            prod = prod.GetById(prod.ProductId);
            if (prod.Error == "") return Json(prod);
            else return StatusCode(StatusCodes.Status422UnprocessableEntity, new { errMsg = prod.Error });
        }

        [HttpPost, Route("/admin/product/ajax-add")]
        public ActionResult AddProduct(Dictionary<string, string> data) {
            Product prod = new Product();
            if (data["name"] != null)
                prod.Name = data["name"];
            if (data["imgUrl"] != null)
                prod.ImgUrl = data["imgUrl"];
            if (data["cost"] != null)
                prod.Cost = decimal.Parse(data["cost"]);
            if (data["price"] != null)
                prod.Price = decimal.Parse(data["price"]);
            if (data["descr"] != null)
                prod.Descr = data["descr"];

            bool success = prod.Insert();
            if (success) return Json(new { msg = "'" + prod.Name + "' added successfully" });
            else return StatusCode(StatusCodes.Status422UnprocessableEntity, new { inputErrors = prod.InputErrors, errMsg = prod.Error });   
        }

        [HttpPost, Route("/admin/product/ajax-update")]
        public ActionResult UpdateProduct(Dictionary<string, string> data) {
            Product prod = new Product();
            if (data["productId"] != null)
                prod.ProductId = int.Parse(data["productId"]);
            else return StatusCode(StatusCodes.Status422UnprocessableEntity, new { errMsg = "Please specify the Product ID" });

            // Verify that the productId from the admin is a valid productId
            // Doing this also allows for the admin to only enter a few fields 
            prod = prod.GetById(prod.ProductId);
            if (prod.Error != "")
                return StatusCode(StatusCodes.Status422UnprocessableEntity, new { errMsg = prod.Error });

            // data entered will be substituted into prod and sent to the backend for validation
            if (data["name"] != null)
                prod.Name = data["name"];
            if (data["imgUrl"] != null)
                prod.ImgUrl = data["imgUrl"];
            if (data["cost"] != null)
                prod.Cost = decimal.Parse(data["cost"]);
            if (data["price"] != null)
                prod.Price = decimal.Parse(data["price"]);
            if (data["descr"] != null)
                prod.Descr = data["descr"];
            if (data["status"] != null)
                prod.Status = data["status"];

            bool success = prod.Update(prod.ProductId);
            if (success) return Json(new { msg = "'" + prod.Name + "' updated successfully" });
            else return StatusCode(StatusCodes.Status422UnprocessableEntity, new { inputErrors = prod.InputErrors, errMsg = prod.Error });
        }

        [HttpGet, Route("/admin/order-report")]
        public ActionResult OrderReport(int? orderId) {
            AdminViewModel model = new AdminViewModel();
            List<Order> orders;
            if (orderId != null) {
                bool getLines = true;
                orders = Order.GetAllOrders((int)orderId, getLines);
            } else {
                bool getLines = false;
                orders = Order.GetAllOrders(getLines);
            }
            model.Json = JsonSerializer.Serialize(orders);
            return View("OrderReport", model);
        }

        [HttpGet, Route("/admin/product-report")]
        public ActionResult ProductReport() {
            AdminViewModel model = new AdminViewModel();
            List<Product> products = Product.GetAllProducts();
            model.Json = JsonSerializer.Serialize(products);
            return View("ProductReport", model);
        }
    }
}