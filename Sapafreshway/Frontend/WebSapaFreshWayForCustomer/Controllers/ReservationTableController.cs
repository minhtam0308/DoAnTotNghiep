using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebSapaFreshWayForCustomer.Models;
using WebSapaFreshWayForCustomer.Services;

namespace WebSapaFreshWayForCustomer.Controllers
{
    public class ReservationTableController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _apiUrl;

        public ReservationTableController(IHttpClientFactory httpClientFactory, ApiService apiService)
        {
            _client = httpClientFactory.CreateClient();
            _apiUrl = apiService.GetApiBaseUrl();
        }

        [HttpGet]
        public async Task<IActionResult> SuggestTables(DateTime reservationDate, string timeSlot, int numberOfGuests)
        {
            var url = $"{_apiUrl}/api/ReservationStaff/tables/suggest-by-areas?reservationDate={reservationDate:yyyy-MM-dd}&timeSlot={Uri.EscapeDataString(timeSlot)}&numberOfGuests={numberOfGuests}";
            var response = await _client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            return Content(json, "application/json");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmWithTables([FromBody] ReservationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .FirstOrDefault()?.ErrorMessage
                    ?? "Dữ liệu không hợp lệ.";

                return BadRequest(new { success = false, message = firstError });
            }

            var dto = new
            {
                model.CustomerName,
                model.Email,
                model.ReservationDate,
                model.ReservationTime,
                model.NumberOfGuests,
                model.Notes,
                model.OtpCode,
                model.PaymentMethod,
                model.TableIds
            };

            var jsonContent = JsonConvert.SerializeObject(dto);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_apiUrl}/api/Reservation/confirm", content);
            var apiJson = await response.Content.ReadAsStringAsync();

            return Content(apiJson, "application/json");
        }
    }
}