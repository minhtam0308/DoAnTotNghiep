using Microsoft.AspNetCore.Mvc;
using WebSapaFreshWay.Models;

namespace WebSapaFreshWay.Controllers
{
    public class LogoController : Controller
    {
        private readonly HttpClient _httpClient;

        public LogoController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5013"); // API backend
        }

        [HttpGet]
        public async Task<IActionResult> Active()
        {
            //var logos = await _httpClient.GetFromJsonAsync<List<SystemLogoViewDto>>("/api/SystemLogo/active");

            //if (logos != null && logos.Count > 0)
            //{
            //    return Json(new { logoUrl = logos[0].LogoUrl });
            //}

            return Json(new { logoUrl = "/img/logo.png" }); // fallback
        }
    }
}
