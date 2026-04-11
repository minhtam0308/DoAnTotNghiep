using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class VerificationService : IVerificationService
    {
        private readonly SapaBackendContext _context;
        private readonly IEmailService _emailService;
        private readonly UserRepository userRepository;
        public VerificationService(SapaBackendContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
            userRepository = new UserRepository(context);
        }

        public async Task<string> GenerateAndSendCodeAsync(int userId, string email, string purpose, int ttlMinutes, CancellationToken ct = default)
        {
            var code = GenerateCode(6);
            var entity = new VerificationCode
            {
                UserId = userId,
                Code = code,
                Purpose = purpose,
                ExpiresAt = DateTime.UtcNow.AddHours(7).AddMinutes(ttlMinutes),
                IsUsed = false
            };
            await _context.VerificationCodes.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);

            await _emailService.SendAsync(email, $"OTP sử dụng để {purpose} SapaFreshWay", $"Mã OTP của bạn là: {code}");
            return code;
        }

        public async Task<bool> VerifyCodeAsync(int userId, string purpose, string code, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var entity = await _context.VerificationCodes
                .Where(v => v.UserId == userId && v.Purpose == purpose && v.Code == code && v.IsUsed == false && v.ExpiresAt >= now)
                .OrderByDescending(v => v.VerificationCodeId)
                .FirstOrDefaultAsync(ct);
            if (entity == null) return false;

            entity.IsUsed = true;
            _context.VerificationCodes.Update(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }


        public async Task<bool> VerifyCodeEmailAsync(string email, string purpose, string code, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var checkEmail = await userRepository.GetByEmailAsync(email);
            var entity = await _context.VerificationCodes
                .Where(v => v.UserId == checkEmail.UserId && v.Purpose == purpose && v.Code == code && v.ExpiresAt >= now)
                .OrderByDescending(v => v.VerificationCodeId)
                .FirstOrDefaultAsync(ct);
            if (entity == null) return false;

            entity.IsUsed = true;
            _context.VerificationCodes.Update(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task InvalidateCodesAsync(int userId, string purpose, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var codes = await _context.VerificationCodes
                .Where(v => v.UserId == userId && v.Purpose == purpose && v.IsUsed == false && v.ExpiresAt >= now)
                .ToListAsync(ct);
            foreach (var c in codes)
            {
                c.IsUsed = true;
            }
            _context.VerificationCodes.UpdateRange(codes);
            await _context.SaveChangesAsync(ct);
        }

        private static string GenerateCode(int length)
        {
            var rng = new Random();
            return string.Concat(Enumerable.Range(0, length).Select(_ => rng.Next(0, 10).ToString()));
        }
    }
}


