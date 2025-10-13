using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Core.DTOS.EmailComposerDtos;
using WorkersManagement.Domain.EmailConfigs;
using WorkersManagement.Infrastructure;

namespace WorkersManagement.Core.Repositories
{
    public class SmtpEmailService(IOptions<EmailSettings> emailSettings,ILogger<SmtpEmailService> logger) : IEmailService
    {
        private readonly EmailSettings _emailSettings = emailSettings.Value;
        private readonly ILogger<SmtpEmailService> _logger = logger;

        public async Task<bool> SendBulkEmailAsync(BulkEmailDto emailDto)
        {
            try
            {
                var sentCount = 0;
                var failedEmails = new List<string>();

                using var client = new MailKit.Net.Smtp.SmtpClient();
                await client.ConnectAsync(_emailSettings.Server, _emailSettings.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);

                foreach (var recipient in emailDto.RecipientEmails)
                {
                    try
                    {
                        var emailMessage = CreateEmailMessage(
                            new List<string> { recipient },
                            emailDto.Subject,
                            emailDto.Body,
                            emailDto.Attachments
                        );

                        await client.SendAsync(emailMessage);
                        sentCount++;

                        _logger.LogInformation("Email sent to {Recipient}", recipient);

                        // Small delay to avoid overwhelming the SMTP server
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
                        failedEmails.Add(recipient);
                    }
                }

                await client.DisconnectAsync(true);

                _logger.LogInformation("Bulk email completed. Sent {SentCount} out of {Total} emails. Failed: {FailedCount}",
                    sentCount, emailDto.RecipientEmails.Count, failedEmails.Count);

                if (failedEmails.Any())
                {
                    _logger.LogWarning("Failed to send emails to: {FailedEmails}", string.Join(", ", failedEmails));
                }

                return sentCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk email");
                return false;
            }
        }

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


        #region
        private MimeMessage CreateEmailMessage(
            List<string> toEmails,
            string subject,
            string message,
            List<EmailAttachmentDto>? attachments = null)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.Username));

            // Add all recipients
            foreach (var email in toEmails)
            {
                emailMessage.To.Add(MailboxAddress.Parse(email));
            }

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

            // Add attachments if any
            if (attachments != null && attachments.Any())
            {
                foreach (var attachment in attachments)
                {
                    var stream = new MemoryStream(attachment.Content);
                    builder.Attachments.Add(attachment.FileName, stream);
                }
            }

            emailMessage.Body = builder.ToMessageBody();
            return emailMessage;
        }
        #endregion
    }
}
