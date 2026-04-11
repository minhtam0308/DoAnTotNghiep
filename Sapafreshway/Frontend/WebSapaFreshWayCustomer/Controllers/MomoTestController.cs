using Microsoft.AspNetCore.Mvc;

namespace WebSapaFreshWay.Controllers
{
    public class MomoTestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
