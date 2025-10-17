using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Dtos.WorkerAuthentication;
using WorkersManagement.Domain.Interfaces;

namespace WorkersManagement.API.Controllers
{

    /// <summary>
    /// Handle worker authentication and password management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorkerAuthController(IWorkersAuthRepository authRepository, ILogger<WorkerAuthController> logger) : ControllerBase
    {
        private readonly IWorkersAuthRepository _authRepository = authRepository;
        private readonly ILogger<WorkerAuthController> _logger = logger;

        /// <summary>
        /// Authenticate worker and generate token
        /// </summary>
        /// <param name="dto">Login credentials</param>
        /// <returns>Authentication token</returns>
        [HttpPost("worker-login")]
        [AllowAnonymous]
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

        /// <summary>
        /// Logout worker
        /// </summary>
        /// <param name="dto">Logout request</param>
        /// <returns>Logout confirmation</returns>
        [HttpPost("worker-logout")]
        [AllowAnonymous]
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

        /// <summary>
        /// Request password reset token
        /// </summary>
        /// <param name="dto">Password reset request</param>
        /// <returns>Reset token confirmation</returns>
        [HttpPost("request-password-reset")]
        [AllowAnonymous]
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


        /// <summary>
        /// Initiate password reset process
        /// </summary>
        /// <param name="dto">Forgot password request</param>
        /// <returns>Reset initiation confirmation</returns>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
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

        /// <summary>
        /// Verify password reset token
        /// </summary>
        /// <param name="dto">Token verification data</param>
        /// <returns>Token verification result</returns>
        [HttpPost("verify-token")]
        [AllowAnonymous]
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

        /// <summary>
        /// Reset password using verified token
        /// </summary>
        /// <param name="dto">Password reset data</param>
        /// <returns>Password reset confirmation</returns>
        [HttpPost("reset-password")]
        [AllowAnonymous]
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
