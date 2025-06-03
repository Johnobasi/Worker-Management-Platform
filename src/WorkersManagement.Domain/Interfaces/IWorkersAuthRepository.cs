using WorkersManagement.Domain.Dtos.WorkerAuthentication;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IWorkersAuthRepository
    {
        Task<string> LoginAsync(LoginDto dto);
        Task LogoutAsync(string email);
        Task RequestPasswordResetAsync(string email);
        Task ResetPasswordAsync(SetPasswordDto dto);
        Task ResetPasswordAsync(string email, string token, string newPassword, string confirmPassword);
        Task VerifyTokenAsync(string email, string token);
    }
}
