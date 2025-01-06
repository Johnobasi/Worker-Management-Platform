using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevotionalController : ControllerBase
    {
        private readonly IDevotionalRepository _devotionalService;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;

        public DevotionalController(IDevotionalRepository devotionalService, IUserRepository userRepository, IEmailService emailService)
        {
            _devotionalService = devotionalService;
            _userRepository = userRepository;
            _emailService = emailService;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadDevotional([FromForm] UploadDevotionalRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("No file uploaded.");

            // Delete old devotional
            var oldDevotionals = await _devotionalService.GetAllDevotionalsAsync();
            foreach (var oldDevotional in oldDevotionals)
            {
                System.IO.File.Delete(oldDevotional.FilePath);
                await _devotionalService.DeleteDevotionalAsync(oldDevotional.Id);
            }

            // Save new devotional
            var filePath = Path.Combine("Devotionals", $"{Guid.NewGuid()}_{request.File.FileName}");
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            var newDevotional = new Devotional
            {
                Id = Guid.NewGuid(),
                FilePath = filePath,
                UploadedAt = DateTime.UtcNow
            };

            await _devotionalService.AddDevotionalAsync(newDevotional);

            // Send email notification to all users
            var users = await _userRepository.GetAllUsersAsync();
            foreach (var user in users)
            {
                await _emailService.SendEmailAsync(user.Email, "New Monthly Devotional Available", "A new monthly devotional has been uploaded. Please check your dashboard to download it.");
            }

            return Ok();
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadDevotional(Guid id)
        {
            var devotional = await _devotionalService.GetDevotionalByIdAsync(id);
            if (devotional == null)
                return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(devotional.FilePath);
            var fileName = Path.GetFileName(devotional.FilePath);
            return File(fileBytes, "application/octet-stream", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDevotionals()
        {
            var devotionals = await _devotionalService.GetAllDevotionalsAsync();
            return Ok(devotionals);
        }

        //DELETE api/devotional/delete/{id}
        [HttpDelete]
        public async Task<IActionResult> DeleteDevotional(Guid id)
        {
            await _devotionalService.DeleteDevotionalAsync(id);
            return Ok();
        }
    }
}
