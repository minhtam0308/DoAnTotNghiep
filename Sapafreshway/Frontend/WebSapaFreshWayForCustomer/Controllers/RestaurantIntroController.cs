using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebSapaFreshWayForCustomer.Services;

namespace WebSapaFreshWayForCustomer.Controllers
{
    public class RestaurantIntroController : Controller
    {
        private string _apiUrl = "http://192.168.79.29:5001/api/Comment";
        private readonly HttpClient _client;
        public RestaurantIntroController(IHttpClientFactory httpClientFactory, ApiService apiService)
        {
            _client = httpClientFactory.CreateClient();
            _apiUrl = $"{apiService.GetApiBaseUrl()}/api/Comment";
        }

        public async Task<IActionResult> Index()
        {
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ViewBag.ApiBase = _apiUrl;
            ViewBag.CanComment = false; 
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            ViewBag.Email = email;
            var response = await _client.GetAsync($"{_apiUrl}/canComment/{customerIdClaim}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var canComment = bool.Parse(content); // hoặc JsonConvert.DeserializeObject<bool>

                if (canComment)
                {
                    ViewBag.CanComment = true;

                }
            }
            return View();
        }
    }
}
