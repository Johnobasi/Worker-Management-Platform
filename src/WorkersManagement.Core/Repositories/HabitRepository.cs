using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Core.Repositories
{
    public class HabitRepository : IHabitRepository
    {
        private readonly WorkerDbContext _context;
        private readonly ILogger<HabitRepository> _logger;
        public HabitRepository(WorkerDbContext workerDbContext,ILogger<HabitRepository> logger)
        {
            _context = workerDbContext;
            _logger = logger;
        }
        public async Task AddHabitAsync(Habit habit)
        {
            try
            {
                _context.Habits.Add(habit);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        public async Task<IEnumerable<Habit>> GetHabitsByTypeAsync(Guid workerId, HabitType type)
        {
            _logger.LogInformation($"Getting habits of type {type} for worker with id {workerId}");
            try
            {
                return await _context.Habits
                    .Where(h => h.WorkerId == workerId && h.Type == type)
                     .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null!;
            }

        }

        public async Task<IEnumerable<Habit>> GetHabitsByWorkerIdAsync(List<Guid> workerIds)
        {
            _logger.LogInformation("Getting habits for worker with id");
            try
            {
                return await _context.Habits
                    .Where(h => workerIds.Contains(h.WorkerId))
                     .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null!;
            }

        }
    }
}
