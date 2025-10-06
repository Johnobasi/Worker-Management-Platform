using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Domain.Dtos.WorkerAuthentication;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;

namespace WorkersManagement.Core.Repositories
{
    public class  WorkerAuthRepository(
        WorkerDbContext context,
        ILogger<WorkerAuthRepository> logger,
        IConfiguration configuration, IEmailService emailService) : IWorkersAuthRepository

      {

            private readonly WorkerDbContext _context = context;
            private readonly ILogger<WorkerAuthRepository> _logger = logger;
            private readonly IConfiguration _configuration = configuration;
            private readonly IEmailService _emailService = emailService;

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
        public async Task SendPasswordSetupEmailAsync(Worker worker)
        {
            _logger.LogInformation("Sending password setup email to {Email}", worker.Email);

            // Generate reset token
            var resetToken = Guid.NewGuid().ToString();
            worker.PasswordResetToken = resetToken;
            worker.PasswordResetTokenExpiration = DateTime.UtcNow.AddHours(24);

            await _context.SaveChangesAsync();

            var baseUrl = _configuration["AppSettings:FrontendBaseUrl"]
              ?? "https://harvestersuk.org";
            
            // Construct reset link safely
            var resetLink = $"{baseUrl.TrimEnd('/')}/verify-token" +
                            $"?email={Uri.EscapeDataString(worker.Email)}" +
                            $"&token={Uri.EscapeDataString(resetToken)}";

            // Load and populate the email template
            var subject = "Set Your Password - Workers CMS";
            string body = await LoadPasswordResetNewWorkerTemplate(worker, resetLink);

            await _emailService.SendEmailAsync(worker.Email, subject, body);
            _logger.LogInformation("Password setup email sent to {Email}", worker.Email);
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
            worker.Status = true;
            worker.IsConfirmed = true;
            await _context.SaveChangesAsync();

            await _context.SaveChangesAsync();
            _logger.LogInformation("Password reset successful");
        }
        public async Task ResetPasswordAsync(string email, string token, string newPassword, string confirmPassword)
        {
            _logger.LogInformation("Password reset attempt for {Email}", email);

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                throw new ArgumentException("All fields are required");

            if (newPassword != confirmPassword)
                throw new ArgumentException("Passwords do not match");

            var worker = await _context.Workers
                .FirstOrDefaultAsync(w => w.Email == email.Trim().ToLower());

            if (worker == null)
            {
                _logger.LogWarning("Password reset failed: Worker with email {Email} not found", email);
                throw new ArgumentException("Invalid email or token");
            }

            if (worker.PasswordResetToken != token || worker.PasswordResetTokenExpiration < DateTime.UtcNow)
            {
                _logger.LogWarning("Password reset failed: Invalid or expired token for {Email}", email);
                throw new ArgumentException("Invalid or expired token");
            }

            worker.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword.Trim());
            worker.PasswordResetToken = null;
            worker.PasswordResetTokenExpiration = null;
            worker.PasswordResetToken = null;
            worker.PasswordResetTokenExpiration = null;
            worker.Status = true;
            worker.IsConfirmed = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset successful for {Email}", email);
        }

        public async Task VerifyTokenAsync(string email, string token)
        {
            _logger.LogInformation("Verifying token for {Email}", email);

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Email and token are required");

            var worker = await _context.Workers
                .FirstOrDefaultAsync(w => w.Email == email.Trim().ToLower());

            if (worker == null)
            {
                _logger.LogWarning("Token verification failed: Worker with email {Email} not found", email);
                throw new ArgumentException("Invalid email or token");
            }

            if (worker.PasswordResetToken != token || worker.PasswordResetTokenExpiration < DateTime.UtcNow)
            {
                _logger.LogWarning("Token verification failed: Invalid or expired token for {Email}", email);
                throw new ArgumentException("Invalid or expired token");
            }

            _logger.LogInformation("Token verified successfully for {Email}", email);
        }
        private string GenerateJwtToken(Worker worker)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, worker.WorkerNumber),
                new(ClaimTypes.Email, worker.Email)
            };

            // Add a Claim for each role
            foreach (var role in worker.Roles.Select(r =>
                             r is Enum ? r.ToString() :
                             r.GetType().GetProperty("Role")?.GetValue(r)?.ToString()))
            {
                if (!string.IsNullOrWhiteSpace(role))
                    claims.Add(new Claim(ClaimTypes.Role, role!));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
        }

        public async Task RequestPasswordResetAsync(string email)
        {
            _logger.LogInformation("Password reset requested for {Email}", email);

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            var worker = await _context.Workers
                .FirstOrDefaultAsync(w => w.Email == email.Trim().ToLower());

            if (worker == null)
            {
                _logger.LogWarning("Password reset failed: Worker with email {Email} not found", email);
                throw new ArgumentException("Email not found");
            }

            // Generate a 6-digit token
            var token = new Random().Next(100000, 999999).ToString();
            worker.PasswordResetToken = token;
            worker.PasswordResetTokenExpiration = DateTime.UtcNow.AddHours(1); // Expires in 1 hour

            await _context.SaveChangesAsync();


            // Load and populate the HTML template
            var subject = "Password Reset Token";
            string body = await LoadAndPopulateEmailTemplate(worker, token);
            await _emailService.SendEmailAsync(email, subject, body);
            _logger.LogInformation("Password reset token sent to {Email}", email);
        }

        private async Task<string> LoadAndPopulateEmailTemplate(Worker worker, string token)
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string templatePath = Path.Combine(baseDirectory, "Templates", "PasswordToken.html");
                if (!File.Exists(templatePath))
                {
                    _logger.LogError("Email template file not found at {Path}", templatePath);
                    throw new FileNotFoundException("Email template file not found", templatePath);
                }
                // Read the HTML template
                string template = await File.ReadAllTextAsync(templatePath);

                // Replace placeholders with actual values
                template = template
                    .Replace("{FirstName}", worker.FirstName)
                    .Replace("{LastName}", worker.LastName)
                    .Replace("{Token}", token);

                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load or populate email template for {Email}", worker.Email);
                throw;
            }
        }

        private async Task<string> LoadPasswordResetNewWorkerTemplate(Worker worker, string resetLink)
        {
            try
            {

                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string templatePath = Path.Combine(baseDirectory, "Templates", "PasswordResetNewWorkerTemplate.html");
                if (!File.Exists(templatePath))
                {
                    _logger.LogError("Email template file not found at {Path}", templatePath);
                    throw new FileNotFoundException("Email template file not found", templatePath);
                }

                string template = await File.ReadAllTextAsync(templatePath);

                template = template
                    .Replace("{FirstName}", worker.FirstName)
                    .Replace("{LastName}", worker.LastName)
                    .Replace("{ResetLink}", resetLink);

                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load or populate email template for {Email}", worker.Email);
                throw;
            }
        }
    }
}
