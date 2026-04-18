using Microsoft.AspNetCore.Mvc;

namespace WebSapaFreshWayForCustomer.Controllers
{
    public class MomoTestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
