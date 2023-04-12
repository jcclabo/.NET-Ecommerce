using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyApp.App.Biz;

namespace MyApp.Controllers
{
    public abstract class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext) {
            if (HttpContext.Session.GetString("admin") != null) {
                ViewBag.Admin = true;
            } else {
                ViewBag.Admin = false;
            }
            if (HttpContext.Session.GetString("customer") != null) {
                ViewBag.SignedIn = true;
            } else {
                ViewBag.SignedIn = false;
            }
            if (HttpContext.Session.GetString("order") != null) {
                
                Order order = new Order();
                string orderJson = HttpContext.Session.GetString("order");
                order = order.Deserialize(orderJson);
                ViewBag.CartProductCount = 0;
                foreach (OrderLine ol in order.Lines) {
                    ViewBag.CartProductCount += ol.Qty;
                }
            } else {
                ViewBag.CartProductCount = 0;
            }
            base.OnActionExecuting(filterContext);
        }
    }
}
