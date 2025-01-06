using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;

namespace WorkersManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserRegistrationTokenRepository _registrationTokenRepository;
        public AdminController(IUserRepository userRepository, IUserRegistrationTokenRepository registrationTokenRepository)
        {
            _userRepository = userRepository;
            _registrationTokenRepository = registrationTokenRepository;
        }

        [HttpPost("setup-password")]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordDto dto)
        {
            var token = await _registrationTokenRepository.GetTokenAsync(dto.Token);
            if (token == null || token.IsUsed || token.ExpiryDate < DateTime.UtcNow)
                return BadRequest("Invalid or expired token.");

            // Hash the password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Update the user's password
            var user = await _userRepository.GetUserByIdAsync(token.UserId);
            if (user == null) 
                return NotFound();

            user.PasswordHash = passwordHash;
            await _userRepository.UpdateUserAsync(user);

            // Mark the token as used
            token.IsUsed = true;
            await _registrationTokenRepository.MarkTokenAsUsedAsync(token);

            return Ok("Password has been set. You can now log in.");
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateNewUserDto dto)
        {
            var user = await _userRepository.CreateUserAsync(dto);
            return Ok(user);
        }

        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            await _userRepository.DeleteUserAsync(id);
            return Ok();
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userRepository.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }

        [HttpPut("update-user")]
        public async Task<IActionResult> UpdateUser([FromBody] User user)
        {
            await _userRepository.UpdateUserAsync(user);
            return Ok();
        }
    }
}
