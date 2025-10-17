using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Infrastructure.EmailComposerDtos;

namespace WorkersManagement.API.Controllers
{
    /// <summary>
    /// Email management endpoints for sending bulk emails
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(IEmailService emailService, ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Send bulk email to multiple recipients
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/email/send
        ///     {
        ///         "subject": "Important Update",
        ///         "body": "Dear user, please be advised of the following update...",
        ///         "recipientEmails": [
        ///             "user1@example.com",
        ///             "user2@example.com"
        ///         ],
        ///         "isHtml": true,
        ///         "ccEmails": [
        ///             "manager@example.com"
        ///         ],
        ///         "bccEmails": [
        ///             "archive@example.com"
        ///         ]
        ///     }
        /// </remarks>
        /// <param name="emailDto">Email data including subject, body, and recipient list</param>
        /// <returns>Result of the bulk email operation</returns>
        /// <response code="200">All emails sent successfully</response>
        /// <response code="207">Some emails failed to send (partial success)</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("send")]
        [AllowAnonymous]
        public async Task<IActionResult> SendEmail([FromBody] BulkEmailDto emailDto)
        {
            if (emailDto == null)
                return BadRequest("Email data cannot be null.");

            if (string.IsNullOrWhiteSpace(emailDto.Subject))
                return BadRequest("Email subject is required.");

            if (string.IsNullOrWhiteSpace(emailDto.Body))
                return BadRequest("Email body is required.");

            if (emailDto.RecipientEmails == null || !emailDto.RecipientEmails.Any())
                return BadRequest("No recipients provided.");

            try
            {
                _logger.LogInformation("Sending bulk email to {Count} recipients", emailDto.RecipientEmails.Count);

                var success = await _emailService.SendBulkEmailAsync(emailDto);

                if (!success)
                {
                    _logger.LogWarning("Bulk email sending completed with errors");
                    return StatusCode(207, new { Message = "Some emails failed to send. Check logs for details." });
                }

                _logger.LogInformation("Bulk email sent successfully to {Count} recipients", emailDto.RecipientEmails.Count);
                return Ok(new { Message = "Emails sent successfully", Count = emailDto.RecipientEmails.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending bulk email");
                return StatusCode(500, "An unexpected error occurred while sending the email.");
            }
        }
    }
}
