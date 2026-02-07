using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    public class SignalRTestController : Controller
    {
        [HttpGet("/signalr-test")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
