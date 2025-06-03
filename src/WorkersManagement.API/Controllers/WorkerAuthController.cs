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
        private readonly ILogger<WorkerAuthController> _logger;

        public WorkerAuthController(IWorkersAuthRepository authRepository, ILogger<WorkerAuthController> logger)
        {
            _authRepository = authRepository;
            _logger = logger;
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



        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            try
            {
                await _authRepository.RequestPasswordResetAsync(dto.Email);
                return Ok(new { Message = "Password reset token sent to your email" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password request");
                return StatusCode(500, new { Message = "An error occurred" });
            }
        }

        [HttpPost("verify-token")]
        public async Task<IActionResult> VerifyToken([FromBody] VerifyTokenDto dto)
        {
            try
            {
                await _authRepository.VerifyTokenAsync(dto.Email, dto.Token);
                return Ok(new { Message = "Token verified successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token verification");
                return StatusCode(500, new { Message = "An error occurred" });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                await _authRepository.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword, dto.ConfirmPassword);
                return Ok(new { Message = "Password reset successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return StatusCode(500, new { Message = "An error occurred" });
            }
        }

    }
}
