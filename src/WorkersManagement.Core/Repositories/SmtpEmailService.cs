using DocumentFormat.OpenXml.Spreadsheet;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Domain.EmailConfigs;
using WorkersManagement.Infrastructure.EmailComposerDtos;

namespace WorkersManagement.Core.Repositories
{
    public class SmtpEmailService(IOptions<EmailSettings> emailSettings,ILogger<SmtpEmailService> logger) : IEmailService
    {
        private readonly EmailSettings _emailSettings = emailSettings.Value;
        private readonly ILogger<SmtpEmailService> _logger = logger;

        public async Task<bool> SendBulkEmailAsync(BulkEmailDto emailDto)
        {
            if (emailDto?.RecipientEmails == null || !emailDto.RecipientEmails.Any())
            {
                _logger.LogWarning("No recipients provided for bulk email");
                return false;
            }

            try
            {
                var sentCount = 0;
                var failedEmails = new List<(string Email, string Error)>();

                using var client = new MailKit.Net.Smtp.SmtpClient();

                 await client.ConnectAsync(_emailSettings.Server, _emailSettings.Port, SecureSocketOptions.StartTls);
                 await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                 var baseMessage = CreateBaseEmailMessage(emailDto.Subject, emailDto.Body, emailDto.Attachments);

                foreach (var recipient in emailDto.RecipientEmails)
                {
                    if (string.IsNullOrWhiteSpace(recipient))
                    {
                        _logger.LogWarning("Skipping empty recipient email");
                        continue;
                    }

                    try
                    {
                        // Clone the base message and set the specific recipient
                        var emailMessage = CloneMessageForRecipient(baseMessage, recipient);

                        await client.SendAsync(emailMessage);
                        sentCount++;

                        _logger.LogInformation("Email sent successfully to {Recipient}", recipient);

                        // Small delay to avoid overwhelming the SMTP server and rate limiting
                        if (emailDto.RecipientEmails.Count > 1)
                        {
                            await Task.Delay(200); // Increased delay for better server handling
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
                        failedEmails.Add((recipient, ex.Message));

                        // If we get a rate limit or connection error, wait longer
                        if (IsRateLimitError(ex))
                        {
                            _logger.LogWarning("Rate limit detected, waiting 5 seconds before continuing...");
                            await Task.Delay(5000);
                        }
                    }
                }

                try
                {
                    await client.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disconnecting from SMTP server");
                }
                _logger.LogInformation(
                    "Bulk email completed. Sent {SentCount} out of {Total} emails. Failed: {FailedCount}",
                    sentCount, emailDto.RecipientEmails.Count, failedEmails.Count
                );
                return sentCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in bulk email sending");
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


        #region Private Methods

        private MimeMessage CreateBaseEmailMessage(string subject, string message, List<EmailAttachmentDto>? attachments = null)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.Username));
            emailMessage.Subject = subject;

            var builder = new BodyBuilder();
            string logoPath = GetLogoPath();
            if (File.Exists(logoPath))
            {
                var logo = builder.LinkedResources.Add(logoPath);
                logo.ContentId = MimeUtils.GenerateMessageId();
                message = message.Replace("/images/Harvesters-Logo.jpeg", $"cid:{logo.ContentId}");
            }

            builder.HtmlBody = message;
            if (attachments != null && attachments.Any())
            {
                foreach (var attachment in attachments)
                {
                    var stream = new MemoryStream(attachment.Content);
                    builder.Attachments.Add(attachment.FileName, stream, ContentType.Parse(attachment.ContentType));
                }
            }

            emailMessage.Body = builder.ToMessageBody();
            return emailMessage;
        }

        private MimeMessage CloneMessageForRecipient(MimeMessage baseMessage, string recipient)
        {
            var clonedMessage = new MimeMessage();
            clonedMessage.From.AddRange(baseMessage.From);
            clonedMessage.Subject = baseMessage.Subject;
            clonedMessage.To.Add(MailboxAddress.Parse(recipient.Trim()));

            clonedMessage.Body = baseMessage.Body;

            return clonedMessage;
        }
        private string GetLogoPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "wwwroot", "images", "Harvesters-Logo.jpeg");
        }

        private bool IsRateLimitError(Exception ex)
        {
            var errorMessage = ex.Message.ToLowerInvariant();
            return errorMessage.Contains("rate limit") ||
                   errorMessage.Contains("too many requests") ||
                   errorMessage.Contains("throttled") ||
                   errorMessage.Contains("exceeded") ||
                   errorMessage.Contains("quota");
        }

        #endregion
    }
}
