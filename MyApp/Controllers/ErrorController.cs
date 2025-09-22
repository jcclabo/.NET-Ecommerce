using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using MyApp.Models;

namespace MyApp.Controllers
{
    public class ErrorController : BaseController
    {
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet, Route("/error")]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
