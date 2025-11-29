using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.API.Controllers
{
    /// <summary>
    /// Manage devotional files and distribution
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DevotionalController : ControllerBase
    {
        private readonly IDevotionalRepository _devotionalService;
        private readonly IWorkerManagementRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<DevotionalController> _logger;

        public DevotionalController(IDevotionalRepository devotionalService, IWorkerManagementRepository userRepository, 
            IEmailService emailService, ILogger<DevotionalController> logger)
        {
            _devotionalService = devotionalService;
            _userRepository = userRepository;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Upload new devotional file (replaces existing ones)
        /// </summary>
        /// <param name="request">Devotional file and data</param>
        /// <returns>Upload result</returns>
        [HttpPost("upload-devotionals")]
        [AllowAnonymous]
        public async Task<IActionResult> UploadDevotional([FromForm] UploadDevotionalRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { Errors = errors });
                }


                if (request.File == null || request.File.Length == 0)
                    return BadRequest("No file uploaded.");

                // Ensure directory exists
                var devotionalFolder = Path.Combine(Directory.GetCurrentDirectory(), "Devotionals");
                if (!Directory.Exists(devotionalFolder))
                    Directory.CreateDirectory(devotionalFolder);

                // Delete old devotionals
                var oldDevotionals = await _devotionalService.GetAllDevotionalsAsync();
                foreach (var oldDevotional in oldDevotionals)
                {
                    var fullOldPath = Path.Combine(Directory.GetCurrentDirectory(), oldDevotional.FilePath);
                    if (System.IO.File.Exists(fullOldPath))
                        System.IO.File.Delete(fullOldPath);

                    await _devotionalService.DeleteDevotionalAsync(oldDevotional.Id);
                }

                // Save new devotional
                var fileName = $"{Guid.NewGuid()}_{request.File.FileName}";
                var fullFilePath = Path.Combine(devotionalFolder, fileName);

                using (var stream = new FileStream(fullFilePath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                var newDevotional = new Devotional
                {
                    Id = Guid.NewGuid(),
                    FilePath = Path.Combine("Devotionals", fileName), // Relative path for storage
                    UploadedAt = DateTime.UtcNow
                };

                await _devotionalService.AddDevotionalAsync(newDevotional);

                // Send email notifications
                var users = await _userRepository.GetAllWorkersAsync();

                foreach (var user in users)
                {
                    var email = user.Email?.Trim();

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        _logger.LogWarning("Skipping worker {WorkerId}: Email is null or empty", user.Id);
                        continue;
                    }

                    if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
                    {
                        _logger.LogWarning("Skipping worker {WorkerId}: Invalid email format: {Email}", user.Id, email);
                        continue;
                    }

                    try
                    {
                        var emailBody = await LoadMonthlyDevotionalsAsync(user);
                        await _emailService.SendEmailAsync(
                            email,
                            "New Monthly Devotional Available",
                            emailBody
                        );

                        _logger.LogInformation("Email sent successfully to {Email}", email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email to {Email}", email);
                    }
                }

                return Ok("Devotional uploaded and notifications sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while uploading devotional.");
                return StatusCode(500, "An error occurred while uploading the devotional.");
            }
            
        }

        /// <summary>
        /// Download devotional file
        /// </summary>
        /// <param name="id">Devotional identifier</param>
        /// <returns>Devotional file</returns>
        [HttpGet("download/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadDevotional(Guid id)
        {
            try
            {
                var devotional = await _devotionalService.GetDevotionalByIdAsync(id);
                if (devotional == null)
                    return NotFound("Devotional not found.");

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), devotional.FilePath);
                if (!System.IO.File.Exists(fullPath))
                    return NotFound("Devotional file is missing.");

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                var fileName = Path.GetFileName(fullPath);

                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while downloading devotional with ID {DevotionalId}", id);
                return StatusCode(500, "An error occurred while downloading the devotional.");
            }

        }

        /// <summary>
        /// Get all available devotionals
        /// </summary>
        /// <returns>List of devotionals</returns>
        [HttpGet("get-all-devotionals")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllDevotionals()
        {
            try
            {
                var devotionals = await _devotionalService.GetAllDevotionalsAsync();
                return Ok(devotionals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all devotionals.");
                return StatusCode(500, "An error occurred while fetching the devotionals.");
            }
        }

        /// <summary>
        /// Delete devotional file
        /// </summary>
        /// <param name="id">Devotional identifier</param>
        /// <returns>Delete result</returns>
        [HttpDelete("delete-devotional/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteDevotional(Guid id)
        {
            try
            {
                var devotional = await _devotionalService.GetDevotionalByIdAsync(id);
                if (devotional == null)
                    return NotFound("Devotional not found.");

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), devotional.FilePath);
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                await _devotionalService.DeleteDevotionalAsync(id);

                return Ok("Devotional deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting devotional with ID {DevotionalId}", id);
                return StatusCode(500, "An error occurred while deleting the devotional.");
            }
        }

        /// <summary>
        /// Preview devotional file in browser
        /// </summary>
        /// <param name="id">Devotional identifier</param>
        /// <returns>Inline devotional preview</returns>
        [HttpGet("preview/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> PreviewDevotional(Guid id)
        {
            try
            {
                var devotional = await _devotionalService.GetDevotionalByIdAsync(id);
                if (devotional == null)
                    return NotFound("Devotional not found.");

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), devotional.FilePath);
                if (!System.IO.File.Exists(fullPath))
                    return NotFound("Devotional file is missing.");

                var fileName = Path.GetFileName(fullPath);
                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

                // Get MIME type with more comprehensive mapping
                var mimeType = GetMimeType(fileExtension);

                // Read file bytes
                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

                // Set content disposition for inline preview
                var contentDisposition = new ContentDispositionHeaderValue("inline")
                {
                    FileName = Uri.EscapeDataString(fileName) // URL encode filename for safety
                };
                Response.Headers.ContentDisposition = contentDisposition.ToString();

                // Add security headers
                Response.Headers.CacheControl = "private, max-age=300"; // Cache for 5 minutes
                Response.Headers.XContentTypeOptions = "nosniff";

                return File(fileBytes, mimeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while previewing devotional with ID {DevotionalId}", id);
                return StatusCode(500, "An error occurred while previewing the devotional.");
            }
        }
        private static string GetMimeType(string fileExtension)
        {
            return fileExtension switch
            {
                // Documents
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",

                // Images
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",

                // Audio
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",

                // Video
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".avi" => "video/x-msvideo",

                _ => "application/octet-stream" // Default for unknown types
            };
        }

        private async Task<string> LoadMonthlyDevotionalsAsync(
           Worker worker)
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string templatePath = Path.Combine(baseDirectory, "Templates", "MonthlyDevotional.html");

                if (!System.IO.File.Exists(templatePath)) 
                {
                    _logger.LogError("Monthly Devotional not found at {Path}", templatePath);
                    throw new FileNotFoundException("Monthly Devotional file not found", templatePath);
                }

                string template = await System.IO.File.ReadAllTextAsync(templatePath);
                
                // Inject template placeholders
                template = template
               .Replace("{FirstName}", worker.FirstName ?? "");

                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load or populate MonthlyDevotional for worker {WorkerId} - {Email}",
                    worker.Id, worker.Email);
                throw;
            }
        }
    }
}
