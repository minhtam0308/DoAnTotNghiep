using Microsoft.AspNetCore.Mvc;

namespace WebSapaFreshWay.Controllers
{
    public class RestaurantIntroController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
