using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace SapaBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            var host = _configuration["EmailSettings:SmtpServer"];
            var port = int.TryParse(_configuration["EmailSettings:SmtpPort"], out var p) ? p : 587;
            var username = _configuration["EmailSettings:SmtpUser"];
            var password = _configuration["EmailSettings:SmtpPass"];
            var from = _configuration["EmailSettings:SmtpFrom"] ?? username;
            var enableSsl = bool.TryParse(_configuration["EmailSettings:SmtpEnableSsl"], out var ssl) ? ssl : true;

            using var message = new MailMessage();
            message.From = new MailAddress(from, "Sapa Fresh Way");
            message.To.Add(new MailAddress(toEmail));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            await client.SendMailAsync(message);
        }
    }
}


