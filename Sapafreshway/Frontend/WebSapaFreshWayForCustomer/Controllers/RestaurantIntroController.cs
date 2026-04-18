using Microsoft.AspNetCore.Mvc;

namespace WebSapaFreshWayForCustomer.Controllers
{
    public class RestaurantIntroController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
