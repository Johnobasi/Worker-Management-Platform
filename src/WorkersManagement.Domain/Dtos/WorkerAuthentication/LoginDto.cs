using System.ComponentModel.DataAnnotations;

namespace WorkersManagement.Domain.Dtos.WorkerAuthentication
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; }
    }

    // Additional DTO for request bodies
    public class LogoutRequestDto
    {
        public string Email { get; set; }
    }

    public class PasswordResetRequestDto
    {
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
    // DTOs
    public class ForgotPasswordDto
    {
        public string Email { get; set; }
    }

    public class VerifyTokenDto
    {
        public string Email { get; set; }
        public string Token { get; set; }
    }
}
