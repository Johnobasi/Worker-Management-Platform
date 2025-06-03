namespace WorkersManagement.Domain.Dtos.WorkerAuthentication
{
    public class LoginDto
    {
        public string Email { get; set; }
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
}
