using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PayOS;
 // WebhookData, verifiedData
using PayOS.Models.Webhooks;
using SapaFreshWayAPI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SapaFreshWayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly OtpService _otpService;
        private readonly IMomoService _momoService;
        private readonly IPayosService _payosService;

        private readonly IConfiguration _config;
        private readonly PayOSClient _payOSClient;

        private readonly UserRepository userRepository;
        private readonly IVerificationService _verificationService;
        private readonly CustomerRepository _customerRepository;


        // ✅ thread-safe
        private static readonly ConcurrentDictionary<string, OtpInfo> _otpCache = new();
        private static readonly ConcurrentDictionary<string, ReservationCreateDto> _pendingReservationCache = new();

        private const decimal DEPOSIT_PER_GUEST = 10000m;

        public ReservationController(
            IReservationService reservationService,
            IMomoService momoService,
            SapaFreshContext context,
            IPayosService payosService,
            IConfiguration config,
            IVerificationService verificationService)
        {
            _reservationService = reservationService;
            _momoService = momoService;
            _payosService = payosService;
            _otpService = new OtpService();
            _verificationService = verificationService;
            userRepository = new UserRepository(context);
            _customerRepository = new CustomerRepository(context);

            _config = config;

            _payOSClient = new PayOSClient(
                _config["PayOS:ClientId"],
                _config["PayOS:ApiKey"],
                _config["PayOS:ChecksumKey"]
            );
        }

        // ====================== SEND OTP ======================
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] string email)
        {
            var now = DateTime.Now;


            var otp = new Random().Next(100000, 999999).ToString();
            var expired = now.AddMinutes(10);

            try
            {
                var checkEmail = await userRepository.GetByEmailAsync(email);
                if (checkEmail == null)
                {
                    await userRepository.CreateAsync(new DomainAccessLayer.Models.User() { Email = email, Status = 0, RoleId = 5, PasswordHash = "User" });
                }
                checkEmail = await userRepository.GetByEmailAsync(email);
                var checkUser = await _customerRepository.GetByUserIdAsync(checkEmail!.UserId);
                if (checkUser == null)
                {
                    var newcus = new Customer()
                    {
                        UserId = checkEmail.UserId,
                        IsVip = false
                    };
                    await _customerRepository.CreateAsync(newcus);
                }

                var code = await _verificationService.GenerateAndSendCodeAsync(checkEmail.UserId, checkEmail.Email, "verify reservation", 10);
                

                return Ok(new { message = "OTP đã gửi", expireAt = expired });

            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            
        }

        // ====================== CONFIRM RESERVATION ======================
        //public class ReservationCreateDtoEmail
        //{
        //    [Required(ErrorMessage = "Tên khách hàng là bắt buộc.")]
        //    public string CustomerName { get; set; } = null!;

        //    [Required(ErrorMessage = "Email là bắt buộc.")]
        //    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        //    public string Email { get; set; } = null!;

        //    [Required(ErrorMessage = "Ngày đặt bàn là bắt buộc.")]
        //    public DateTime ReservationDate { get; set; }

        //    [Required(ErrorMessage = "Giờ đặt bàn là bắt buộc.")]
        //    public DateTime ReservationTime { get; set; }

        //    [Range(1, 50, ErrorMessage = "Số lượng khách phải ít nhất 1 người.")]
        //    public int NumberOfGuests { get; set; }

        //    public string? Notes { get; set; }

        //    [Required(ErrorMessage = "OTP là bắt buộc.")]
        //    public string? OtpCode { get; set; }

        //    /// <summary>
        //    /// MOMO hoặc PAYOS (default PAYOS)
        //    /// </summary>
        //    public string PaymentMethod { get; set; } = "PAYOS";
        //}

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmReservation([FromBody] ReservationCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

            var ok = await _verificationService.VerifyCodeEmailAsync(dto.Email, "verify reservation", dto.OtpCode);
            if (!ok)
                return BadRequest(new { message = "OTP không hợp lệ." });

            decimal requiredDeposit = dto.NumberOfGuests * DEPOSIT_PER_GUEST;

            try
            {
                // ✅ lấy từ config
                string returnUrl = _config["PayOS:ReturnUrl"];
                string cancelUrl = _config["PayOS:CancelUrl"];
                string webhookUrl = _config["PayOS:WebhookUrl"]; // để bạn trả về cho FE/Log, PayOS gọi theo cấu hình dashboard

                string paymentMethod = dto.PaymentMethod?.ToUpper() ?? "PAYOS";
                string orderId;
                string orderInfo;
                string payUrl;

                if (paymentMethod == "MOMO")
                {
                    orderId = Guid.NewGuid().ToString("N");
                    orderInfo = $"Coc {dto.NumberOfGuests} khach";

                    payUrl = await _momoService.CreatePaymentAsync(requiredDeposit, orderId, orderInfo);
                }
                else
                {
                    // ✅ giảm nguy cơ trùng
                    long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    orderId = orderCode.ToString();

                    orderInfo = "Tien coc dat ban";

                    // ipnUrl param bạn đang truyền vào service: SDK payOS thường cấu hình webhook ở dashboard,
                    // nhưng cứ truyền để giữ interface không đổi.
                    payUrl = await _payosService.CreatePaymentAsync(requiredDeposit, orderId, orderInfo, returnUrl, webhookUrl);
                    paymentMethod = "PAYOS";
                }

                _pendingReservationCache[orderId] = dto;

                return Ok(new
                {
                    success = true,
                    message = "Vui lòng thanh toán tiền cọc.",
                    orderId,
                    requiredDeposit,
                    payUrl,
                    paymentMethod,
                    returnUrl,
                    cancelUrl
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ====================== IPN MOMO ======================
        [HttpPost("momo-ipn")]
        public async Task<IActionResult> MomoIpn([FromBody] MomoIpnRequest ipn)
        {
            if (!_pendingReservationCache.TryGetValue(ipn.orderId, out _))
                return Ok(new { message = "orderId not found" });

            if (ipn.resultCode != 0)
                return Ok(new { message = "payment failed" });

            return await HandlePaymentSuccessAsync(
                ipn.orderId,
                decimal.Parse(ipn.amount),
                "MOMO",
                ipn.transId);
        }

        // ====================== IPN PAYOS (VERIFY SIGNATURE) ======================
        // ✅ nhận WebhookData đúng theo SDK, rồi VerifyAsync
        [HttpPost("payos-ipn")]
        public async Task<IActionResult> PayosIpn()
        {
            Console.WriteLine("=== PAYOS IPN HIT (RAW) ===");

            string raw;
            using (var reader = new StreamReader(Request.Body))
                raw = await reader.ReadToEndAsync();

            Console.WriteLine("RAW=" + raw);

            if (string.IsNullOrWhiteSpace(raw))
                return BadRequest(new { message = "empty body" });

            Webhook? webhook;
            try
            {
                webhook = JsonSerializer.Deserialize<Webhook>(raw, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("DESERIALIZE FAIL: " + ex.Message);
                return BadRequest(new { message = "invalid json" });
            }

            if (webhook?.Data == null)
            {
                Console.WriteLine("-> TEST WEBHOOK (no data)");
                return Ok(new { message = "webhook test ok" });
            }

            try
            {
                var verified = await _payOSClient.Webhooks.VerifyAsync(webhook);
                Console.WriteLine("-> VERIFY OK");

                var orderId = verified.OrderCode.ToString();
                Console.WriteLine("orderId=" + orderId);

                if (!_pendingReservationCache.TryGetValue(orderId, out _))
                {
                    Console.WriteLine("-> orderId NOT FOUND in pending cache");
                    // nên trả 200 để PayOS không đánh fail, nhưng bạn sẽ biết nguyên nhân
                    return Ok(new { message = "orderId not found" });
                }

                Console.WriteLine("-> CALL HandlePaymentSuccessAsync");
                return await HandlePaymentSuccessAsync(orderId,
                    Convert.ToDecimal(verified.Amount),
                    "PAYOS",
                    verified.Reference);
            }
            catch (Exception ex)
            {
                Console.WriteLine("-> VERIFY FAIL: " + ex.Message);

                // LÚC DEV: trả 400 để PayOS dashboard biết webhook lỗi thật
                // Khi demo xong có thể đổi lại Ok()
                return BadRequest(new { message = "verify failed", detail = ex.Message });
            }
        }



        // ============== API khách hàng lấy danh sách đặt bàn ==============
        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetReservationsByCustomer(int customerId)
        {
            var result = await _reservationService.GetReservationsByCustomerAsync(customerId);
            return Ok(result);
        }

        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdateReservation(int id, [FromBody] ReservationUpdateDto dto)
        {
            try
            {
                var result = await _reservationService.UpdateReservationAsync(id, dto);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("Cancel/{id}")]
        public async Task<IActionResult> CancelReservation(int id)
        {
            try
            {
                var result = await _reservationService.CancelReservationByCustomerAsync(id);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ====================== HANDLE SUCCESS ======================
        private async Task<IActionResult> HandlePaymentSuccessAsync(
            string orderId,
            decimal amount,
            string method,
            string transId)
        {
            if (!_pendingReservationCache.TryGetValue(orderId, out var dto))
                return Ok(new { message = "orderId not found" });

            var reservation = await _reservationService.CreateReservationAsync(dto);

            if (dto.TableIds != null && dto.TableIds.Count > 0)
            {
                try
                {
                    await _reservationService.AssignTablesAsync(new AssignTableDto
                    {
                        ReservationId = reservation.ReservationId,
                        TableIds = dto.TableIds,
                        StaffId = 1,
                        ConfirmBooking = false
                    });
                }
                catch (Exception tableEx)
                {
                    Console.WriteLine("Assign tables failed: " + tableEx.Message);
                }
            }

            var deposit = new DomainAccessLayer.Models.ReservationDeposit
            {
                ReservationId = reservation.ReservationId,
                Amount = amount,
                PaymentMethod = method,
                DepositCode = transId,
                DepositDate = DateTime.Now,
                Notes = $"Thanh toán đặt cọc qua {method}"
            };

            await _reservationService.AddDepositAsync(reservation.ReservationId, deposit);

            reservation.DepositPaid = true;
            reservation.DepositAmount = amount;
            reservation.TotalDepositPaid = amount;
            reservation.RequireDeposit = true;
            reservation.Status = "Pending";

            await _reservationService.UpdateReservationDepositStatusAsync(reservation);

            _pendingReservationCache.TryRemove(orderId, out _);

            return Ok(new { message = "reservation created", reservationId = reservation.ReservationId });
        }

        [HttpGet("active/{tableId}")]
        public async Task<IActionResult> GetActiveReservation(int tableId)
        {
            var reservationId = await _reservationService.GetActiveReservationIdByTableAsync(tableId);

            if (reservationId == null)
            {
                return NotFound(new { message = "Bàn này hiện chưa có khách Check-in." });
            }

            return Ok(new { reservationId = reservationId });
        }
    }

    public class OtpInfo
    {
        public string OtpCode { get; set; }
        public DateTime Expired { get; set; }
        public int DailyCount { get; set; }
        public DateTime LastSent { get; set; }
        public List<DateTime> Timestamps { get; set; } = new();
    }
}
