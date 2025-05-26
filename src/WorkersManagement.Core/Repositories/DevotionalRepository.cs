using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Core.Repositories
{
    public class DevotionalRepository : IDevotionalRepository
    {
        private readonly WorkerDbContext _context;
        private readonly ILogger<DevotionalRepository> _logger;
        public DevotionalRepository(WorkerDbContext workerDbContext,ILogger<DevotionalRepository> logger)
        {
            _context = workerDbContext;
            _logger = logger;
        }
        public async Task AddDevotionalAsync(Devotional devotional)
        {
            try
            {
                await _context.Devotionals.AddAsync(devotional);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);            
            }

        }

        public async Task DeleteDevotionalAsync(Guid id)
        {
            _logger.LogInformation($"Deleting devotional with id: {id}");
            try
            {
                var devotional = await GetDevotionalByIdAsync(id);
                if (devotional != null)
                {
                    _context.Devotionals.Remove(devotional);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        public async Task<IEnumerable<Devotional>> GetAllDevotionalsAsync()
        {
            try
            {
                return await _context.Devotionals.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null!;
            }

        }

        public async Task<Devotional?> GetDevotionalByIdAsync(Guid id)
        {
            _logger.LogInformation($"Getting devotional with id: {id}");
            try
            {
                return await _context.Devotionals.FirstOrDefaultAsync(d => d.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null!;
            }

        }
    }
}
