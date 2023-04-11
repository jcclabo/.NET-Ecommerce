using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using MyApp.Models;

namespace MyApp.Controllers
{
    public class SharedController : BaseController
    {
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet, Route("/Error")]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
