using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Core.Repositories
{
    public class UserRepository(WorkerDbContext workerDbContext, IEmailService emailService,
        IUserRegistrationTokenRepository userRegistrationTokenRepository, IConfiguration configuration,
        ILogger<UserRepository> logger, IDepartmentRepository departmentRepository) : IUserRepository
    {
        private readonly WorkerDbContext _context = workerDbContext;
        private readonly IEmailService _emailService = emailService;
        private readonly IUserRegistrationTokenRepository _registrationTokenRepository = userRegistrationTokenRepository;
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<UserRepository> _logger = logger;
        private readonly IDepartmentRepository _departmentRepository = departmentRepository;
        public async Task<User> CreateUserAsync(CreateNewUserDto dto)
        {
            _logger.LogInformation($"Creating new user {dto.Email}");

            if (string.IsNullOrEmpty(dto.Email) || _context.Users.Any(u => u.Email == dto.Email))
                throw new InvalidOperationException("Email is required or user with this email already exists.");

            var department = dto.DepartmentId.HasValue ? await _departmentRepository.GetDepartmentByNameAsync(dto.FirstName) : null;
            if (dto.Role == UserRole.Worker && department == null)
                throw new ArgumentException("Department is required for a Worker role.");

            try
            {
                // Create the user in the database
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Role = dto.Role,
                    DepartmentId = dto.DepartmentId,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Users.AddAsync(user);

                // Generate a registration token
                var token = Guid.NewGuid().ToString();
                var registrationToken = new UserRegistrationToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = token,
                    ExpiryDate = DateTime.UtcNow.AddDays(1), // Token expires in 1 day
                    IsUsed = false
                };
                await _registrationTokenRepository.SaveTokenAsync(registrationToken);

                // Send email
                var setupLink = $"{_configuration["App:BaseUrl"]}/setup-password?token={token}";
                var emailBody = $@"
                <p>Hello,</p>
                <p>Your account has been created. Please <a href='{setupLink}'>click here</a> to set up your password.</p>
                <p>This link will expire in 24 hours.</p>";

                await _emailService.SendEmailAsync(user.Email, "Set Up Your Password", emailBody);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw; // Re-throw the exception to ensure the method always returns a value or throws an exception
            }
        }

        public async Task DeleteUserAsync(Guid id)
        {
            try
            {
                var user = await GetUserByIdAsync(id);
                if (user != null)
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        public async Task<ICollection<User>> GetAllUsersAsync()
        {
            try
            {
                return await _context.Users.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<User>();
            }

        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            try
            {
                return await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }

        }

        public Task<bool> SetPasswordAsync(SetPasswordDto dto)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateUserAsync(User user)
        {
            try
            {
                _context.Set<User>().Update(user);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }
    }
}
