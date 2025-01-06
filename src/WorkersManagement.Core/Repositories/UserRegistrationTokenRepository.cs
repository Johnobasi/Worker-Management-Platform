using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Core.Repositories
{
    public class UserRegistrationTokenRepository : IUserRegistrationTokenRepository
    {
        private readonly WorkerDbContext _context;
        private readonly ILogger<UserRegistrationTokenRepository> _logger;
        public UserRegistrationTokenRepository(WorkerDbContext workerDbContext, ILogger<UserRegistrationTokenRepository> logger)
        {
            _context = workerDbContext;
            _logger = logger;
        }
        public async Task<UserRegistrationToken?> GetTokenAsync(string token)
        {
            try
            {
                return await _context.UserRegistrationTokens
                 .Include(t => t.UserId)
                     .FirstOrDefaultAsync(t => t.Token == token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }

        }

        public async Task MarkTokenAsUsedAsync(UserRegistrationToken token)
        {
            try
            {
                token.IsUsed = true;
                _context.UserRegistrationTokens.Update(token);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        public async Task SaveTokenAsync(UserRegistrationToken token)
        {
            try
            {
                _context.UserRegistrationTokens.Add(token);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }
    }
}
