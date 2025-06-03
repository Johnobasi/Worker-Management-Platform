using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Dtos.WorkerAuthentication;
using WorkersManagement.Domain.Interfaces;

namespace WorkersManagement.API.Controllers
{


    [ApiController]
    [Route("api/[controller]")]
    public class WorkerAuthController : ControllerBase
    {
        private readonly IWorkersAuthRepository _authRepository;

        public WorkerAuthController(IWorkersAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        [HttpPost("worker-login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var token = await _authRepository.LoginAsync(dto);
                return Ok(new { Token = token });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("worker-logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto)
        {
            try
            {
                await _authRepository.LogoutAsync(dto.Email);
                return Ok(new { Message = "Logout successful" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequestDto dto)
        {
            try
            {
                await _authRepository.RequestPasswordResetAsync(dto.Email);
                return Ok(new { Message = "Password reset token generated. Check your email." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] SetPasswordDto dto)
        {
            try
            {
                await _authRepository.ResetPasswordAsync(dto);
                return Ok(new { Message = "Password reset successful" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
