using Azure.Core;
using BusinessAccessLayer.DTOs.Customers;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SapaBackend.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Customer")] 
    public class CustomerController : ControllerBase
    {
        private readonly SapaBackendContext _context;
        private readonly IConfiguration _configuration;
        private readonly ICustomerManagementService _customerManagementService;

        private readonly IVerificationService _verificationService;
        private readonly IEmailService _emailService;
        private readonly UserRepository userRepository;
        private static Dictionary<string, OtpInfo> _otpCache = new();
        private static Dictionary<string, ContactOtpInfo> _contactOtpCache = new();

        public CustomerController(
            SapaBackendContext context,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            ICustomerManagementService customerManagementService,
            IVerificationService verificationService)
        {
            _context = context;
            _configuration = configuration;
            _customerManagementService = customerManagementService;
            _verificationService = verificationService;
            userRepository = new UserRepository(context);
        }

        public class OtpInfo
        {
            public string OtpCode { get; set; } = string.Empty;
            public DateTime Expired { get; set; }
            public int DailyCount { get; set; }
            public DateTime LastSent { get; set; }
            public List<DateTime> Timestamps { get; set; } = new();
        }

        private class ContactOtpInfo
        {
            public string OtpCode { get; set; } = string.Empty;
            public DateTime Expired { get; set; }
            public int DailyCount { get; set; }
            public DateTime LastSent { get; set; }
            public List<DateTime> Timestamps { get; set; } = new();
        }

        // Anonymous: request OTP for phone login (mirrors ReservationController limits)
        [HttpPost("send-otp-login")]
        [AllowAnonymous]
        public async Task<IActionResult> SendOtpLogin([FromBody] string email)
        {
            var now = DateTime.Now;
            var expired = now.AddMinutes(10);
            try
            {
                var checkEmail = await userRepository.GetByEmailAsync(email);
            if (checkEmail == null) {
                await userRepository.AddAsync(new DomainAccessLayer.Models.User() { Email = email, Status = 0, RoleId = 5 });
            }
                var checkEmail2 = await userRepository.GetByEmailAsync(email);

                var code = await _verificationService.GenerateAndSendCodeAsync(checkEmail2.UserId, checkEmail2.Email, "logincustomer", 10);
                //var subject = "Cảm ơn quý khách đã đặt bàn tại SapaFreshWay";
                //var body = $@"
                //                <div style='font-family:Segoe UI,Helvetica,Arial,sans-serif;font-size:14px;'>
                //                  <p>Chào quý khách,</p>
                //                  <p><strong>Thông tin đăng nhập:</strong></p>
                //                  <ul>
                //                    <li>Email: <strong>{email}</strong></li>
                //                  </ul>
                //                  <p>Vui lòng đăng nhập mã OTP của quý khách là: {code}</p>
                //                  <p>Trân trọng,</p>
                //                  <p>Sapa Fresh Way RMS</p>
                //                  <hr />
                //                  <small>Đây là email tự động, vui lòng không trả lời.</small>
                //                </div>";
                //    await _emailService.SendAsync(email, subject, body);
                return Ok(new { message = "OTP đã được gửi.", expireAt = expired });

            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        public class VerifyLoginDto { public string Email { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; }

        // Anonymous: verify OTP and return JWT for Customer
        [HttpPost("verify-otp-login")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtpLogin([FromBody] VerifyLoginDto dto, CancellationToken ct)
        {
            var ok = await _verificationService.VerifyCodeEmailAsync(dto.Email, "logincustomer", dto.Code, ct);
            if (!ok)
                return BadRequest(new { message = "OTP không hợp lệ." });

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsDeleted == false, ct);
            //if (user == null || user.RoleId != 5)
            //    return Unauthorized(new { message = "Tài khoản không hợp lệ" });

            //  FIX: Không cho phép đăng nhập nếu tài khoản đã bị vô hiệu hóa (Status = 1 = Inactive)
            if (user.Status == 1)
            {
                return Unauthorized(new { message = "Tài khoản này đang không còn hoạt động trên hệ thống. Vui lòng liên hệ quản trị viên để được kích hoạt lại." });
            }


            // Issue JWT token for Customer
            var jwtSection = _configuration.GetSection("Jwt");
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSection["Key"] ?? "replace-with-strong-key"));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new System.Security.Claims.Claim("userId", user.UserId.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.FullName ?? user.Email),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email ?? string.Empty),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role?.RoleName ?? "Customer")
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = jwtSection["Issuer"],
                Audience = jwtSection["Audience"],
                SigningCredentials = creds
            };
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            return Ok(new
            {
                userId = user.UserId,
                fullName = user.FullName,
                email = user.Email,
                roleId = user.RoleId,
                roleName = user.Role?.RoleName ?? "Customer",
                token = jwt
            });
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile(CancellationToken ct)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var userId = int.Parse(userIdClaim);

            var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);
            if (customer == null) return NotFound(new { message = "Customer not found" });

            var profile = await _customerManagementService.GetCustomerProfileAsync(customer.CustomerId, ct);
            if (profile == null) return NotFound(new { message = "Profile not found" });

            return Ok(profile);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] CustomerProfileUpdateRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var userId = int.Parse(userIdClaim);

            var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);
            if (customer == null) return NotFound(new { message = "Customer not found" });

            var updated = await _customerManagementService.UpdateCustomerProfileAsync(customer.CustomerId, request, ct);
            if (updated == null) return StatusCode(500, new { message = "Failed to update profile" });

            return Ok(updated);
        }

        // ====================== CHANGE EMAIL (OTP via Email) ======================
        public class SendChangeEmailOtpRequest
        {
            [Required, EmailAddress, StringLength(100)]
            public string Email { get; set; } = string.Empty;
        }

        public class VerifyChangeEmailOtpRequest
        {
            [Required, EmailAddress, StringLength(100)]
            public string Email { get; set; } = string.Empty;

            [Required, StringLength(10)]
            public string Code { get; set; } = string.Empty;
        }

        [HttpPost("profile/change-email/send-otp")]
        public async Task<IActionResult> SendChangeEmailOtp([FromBody] SendChangeEmailOtpRequest req, CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var newEmail = req.Email.Trim();
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId && u.IsDeleted == false, ct);
            if (user == null) return NotFound(new { message = "User not found" });

            if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Email mới trùng với email hiện tại." });

            var exists = await _context.Users.AsNoTracking().AnyAsync(u => u.IsDeleted == false && u.UserId != userId && u.Email == newEmail, ct);
            if (exists) return BadRequest(new { message = "Email này đã được sử dụng." });

            // Bind code to this exact target email (avoid using a code to set a different email)
            var purpose = $"ChangeEmail:{newEmail.ToLowerInvariant()}";
            await _verificationService.InvalidateCodesAsync(userId, purpose, ct);
            await _verificationService.GenerateAndSendCodeAsync(userId, newEmail, purpose, ttlMinutes: 10, ct);

            return Ok(new { message = "Mã OTP đã được gửi đến email mới.", expireMinutes = 10 });
        }

        [HttpPost("profile/change-email/verify")]
        public async Task<IActionResult> VerifyChangeEmailOtp([FromBody] VerifyChangeEmailOtpRequest req, CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var newEmail = req.Email.Trim();
            var purpose = $"ChangeEmail:{newEmail.ToLowerInvariant()}";

            var ok = await _verificationService.VerifyCodeAsync(userId, purpose, req.Code.Trim(), ct);
            if (!ok) return BadRequest(new { message = "Mã OTP không đúng hoặc đã hết hạn." });

            var exists = await _context.Users.AsNoTracking().AnyAsync(u => u.IsDeleted == false && u.UserId != userId && u.Email == newEmail, ct);
            if (exists) return BadRequest(new { message = "Email này đã được sử dụng." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.IsDeleted == false, ct);
            if (user == null) return NotFound(new { message = "User not found" });

            user.Email = newEmail;
            user.ModifiedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync(ct);

            return Ok(new { message = "Xác thực OTP thành công. Email đã được cập nhật." });
        }

        // ====================== CHANGE PHONE (OTP via SMS) ======================
        public class SendChangePhoneOtpRequest
        {
            [Required, Phone, StringLength(20)]
            public string Phone { get; set; } = string.Empty;
        }

        public class VerifyChangePhoneOtpRequest
        {
            [Required, Phone, StringLength(20)]
            public string Phone { get; set; } = string.Empty;

            [Required, StringLength(10)]
            public string Code { get; set; } = string.Empty;
        }


        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] string? status, CancellationToken ct)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var userId = int.Parse(userIdClaim);

            var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);
            if (customer == null) return Ok(new object[] { });

            var query = _context.Orders.AsNoTracking().Where(o => o.CustomerId == customer.CustomerId);
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var items = await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
            return Ok(items);
        }
    }
}


