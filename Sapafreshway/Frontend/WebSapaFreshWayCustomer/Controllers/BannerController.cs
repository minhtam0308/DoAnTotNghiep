using Microsoft.AspNetCore.Mvc;
using   WebSapaFreshWay.Models;

namespace WebSapaFreshWay.Controllers
{
    public class BannerController : Controller
    {
        private readonly HttpClient _httpClient;

        public BannerController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5013");
        }

        [HttpGet]
        public async Task<IActionResult> Active()
        {
            //var banners = await _httpClient.GetFromJsonAsync<List<BrandBannerViewDto>>("/api/BrandBanner/active");
            return Json( new List<BrandBannerViewDto>());
        }
    }
}
