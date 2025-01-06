using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Domain.EmailConfigs;

namespace WorkersManagement.Core.Repositories
{
    public class SmtpEmailService(IOptions<EmailConfiguration> emailConfig, ILogger<SmtpEmailService> logger) : IEmailService
    {
        private readonly EmailConfiguration _emailConfig = emailConfig.Value;
        private readonly ILogger<SmtpEmailService> _logger = logger;

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            _logger.LogInformation($"Sending email to {to}");
            using var smtpClient = new SmtpClient(_emailConfig.SmtpServer)
            {
                Port = _emailConfig.Port,
                Credentials = new NetworkCredential(_emailConfig.Username, _emailConfig.Password),
                EnableSsl = true
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailConfig.FromAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email to {to}: {ex.Message}");
                throw;
            }
        }
    }
}
