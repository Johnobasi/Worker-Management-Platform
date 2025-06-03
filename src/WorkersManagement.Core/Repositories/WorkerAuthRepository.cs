using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WorkersManagement.Domain.Dtos.WorkerAuthentication;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;

namespace WorkersManagement.Core.Repositories
{
    public class  WorkerAuthRepository(
        WorkerDbContext context,
        ILogger<WorkerAuthRepository> logger,
        IConfiguration configuration) : IWorkersAuthRepository
      {

            private readonly WorkerDbContext _context = context;
            private readonly ILogger<WorkerAuthRepository> _logger = logger;
            private readonly IConfiguration _configuration = configuration;

        public async Task<string> LoginAsync(LoginDto dto)
            {
                _logger.LogInformation("Attempting login for user: {Email}", dto.Email);

                var worker = await _context.Workers
                    .FirstOrDefaultAsync(w => w.Email == dto.Email);

                if (worker == null)
                {
                    _logger.LogWarning("Login failed: Worker with email {Email} not found", dto.Email);
                    throw new UnauthorizedAccessException("Invalid credentials");
                }

                dto.Password = dto.Password.Trim();
                if (!BCrypt.Net.BCrypt.Verify(dto.Password, worker.PasswordHash))
                {
                    _logger.LogWarning("Login failed: Invalid password for {Email}", dto.Email);
                    throw new UnauthorizedAccessException("Invalid credentials");
                }

                worker.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(worker);
                _logger.LogInformation("Login successful for {Email}", dto.Email);
                return token;
            }

            public async Task LogoutAsync(string email)
            {
                _logger.LogInformation("Logging out user: {Email}", email);

                var worker = await _context.Workers
                    .FirstOrDefaultAsync(w => w.Email == email);

                if (worker == null)
                {
                    _logger.LogWarning("Logout failed: Worker with email {Email} not found", email);
                    throw new ArgumentException("Worker not found");
                }

                worker.LastLogin = null;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Logout successful for {Email}", email);
            }

            public async Task RequestPasswordResetAsync(string email)
            {
                _logger.LogInformation("Requesting password reset for {Email}", email);

                var worker = await _context.Workers
                    .FirstOrDefaultAsync(w => w.Email == email);

                if (worker == null)
                {
                    _logger.LogWarning("Password reset request failed: Worker with email {Email} not found", email);
                    throw new ArgumentException("Worker not found");
                }

                var resetToken = Guid.NewGuid().ToString();
                worker.PasswordResetToken = resetToken;
                worker.PasswordResetTokenExpiration = DateTime.UtcNow.AddHours(1);

                await _context.SaveChangesAsync();

                // In a real implementation, send email with reset token
                _logger.LogInformation("Password reset token generated for {Email}", email);
            }

            public async Task ResetPasswordAsync(SetPasswordDto dto)
            {
                _logger.LogInformation("Attempting password reset for token");

                var worker = await _context.Workers
                    .FirstOrDefaultAsync(w => w.PasswordResetToken == dto.Token);

                if (worker == null)
                {
                    _logger.LogWarning("Password reset failed: Invalid token");
                    throw new ArgumentException("Invalid or expired reset token");
                }

                if (worker.PasswordResetTokenExpiration < DateTime.UtcNow)
                {
                    _logger.LogWarning("Password reset failed: Token expired");
                    throw new ArgumentException("Invalid or expired reset token");
                }

                worker.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                worker.PasswordResetToken = null;
                worker.PasswordResetTokenExpiration = null;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Password reset successful");
            }

            private string GenerateJwtToken(Worker worker)
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(
                    [
                    new Claim(ClaimTypes.NameIdentifier, worker.WorkerNumber),
                    new Claim(ClaimTypes.Email, worker.Email),
                    new Claim(ClaimTypes.Role, worker.Role.ToString())
                ]),
                    Expires = DateTime.UtcNow.AddHours(24),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
    }
}
