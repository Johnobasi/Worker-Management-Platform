using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Domain.EmailConfigs;

namespace WorkersManagement.Core.Repositories
{
    public class SmtpEmailService(IOptions<EmailSettings> emailSettings) : IEmailService
    {
        private readonly EmailSettings _emailSettings = emailSettings.Value;

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.Username));
            emailMessage.To.Add(MailboxAddress.Parse(toEmail));
            emailMessage.Subject = subject;

            var builder = new BodyBuilder();

            // Embed logo from wwwroot/images
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string logoPath = Path.Combine(baseDir, "wwwroot", "images", "Harvesters-Logo.jpeg");

            if (File.Exists(logoPath))
            {
                var logo = builder.LinkedResources.Add(logoPath);
                logo.ContentId = MimeUtils.GenerateMessageId();

                // Replace <img src="/images/..."> with cid version
                message = message.Replace("/images/Harvesters-Logo.jpeg", $"cid:{logo.ContentId}");
            }

            builder.HtmlBody = message;
            emailMessage.Body = builder.ToMessageBody();

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(_emailSettings.Server, _emailSettings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
        }


    }
}
