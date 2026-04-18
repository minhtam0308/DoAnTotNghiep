using Microsoft.AspNetCore.Mvc;
using WebSapaFreshWayForCustomer.Models;
using WebSapaFreshWayForCustomer.Services;

namespace WebSapaFreshWayForCustomer.Controllers
{
    public class BannerController : Controller
    {
        private readonly HttpClient _httpClient;

        public BannerController(IHttpClientFactory httpClientFactory, ApiService apiService)
        {
            _httpClient = httpClientFactory.CreateClient();
            //_httpClient.BaseAddress = new Uri("https://localhost:7096");
            _httpClient.BaseAddress = new Uri($"{apiService.GetApiBaseUrl()}");

        }

        [HttpGet]
        public async Task<IActionResult> Active()
        {
            var banners = await _httpClient.GetFromJsonAsync<List<BrandBannerViewDto>>("/api/BrandBanner/active");
            return Json(banners ?? new List<BrandBannerViewDto>());
        }
    }
}
