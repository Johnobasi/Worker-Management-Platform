using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.API.Controllers
{
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

        [HttpPost("upload-devotionals")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UploadDevotional([FromForm] UploadDevotionalRequest request)
        {
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
                await _emailService.SendEmailAsync(
                    user.Email,
                    "New Monthly Devotional Available",
                    "A new monthly devotional has been uploaded. Please check your dashboard to download it."
                );
            }

            return Ok("Devotional uploaded and notifications sent.");
        }


        [HttpGet("download/{id}")]
        [Authorize(Policy = "Worker")]
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


        [HttpGet("get-all-devotionals")]
        [Authorize(Policy = "Worker")]
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


        [HttpDelete("delete-devotional/{id}")]
        [Authorize(Policy = "Admin")]
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


    }
}
