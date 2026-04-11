using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebSapaFreshWay.Models;

namespace WebSapaFreshWay.Controllers
{
    [Authorize(Roles = "Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _httpClient;
        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5013/");
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                // API 
                var bestSellers = await _httpClient.GetFromJsonAsync<List<MenuItemDto>>("api/MenuItem/top-best-sellers");
                
                //  sang View
                ViewBag.BestSellers = bestSellers ?? new List<MenuItemDto>();
                ViewBag.Combos = new List<ComboDto>();
                ViewBag.Events = new List<EventDto>();
            }
            catch (Exception ex)
            {
                ViewBag.BestSellers = new List<MenuItemDto>();
                ViewBag.Combos = new List<ComboDto>();
            }

            return View();

        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
