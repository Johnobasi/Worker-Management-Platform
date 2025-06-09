using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkersManagement.Domain.Dtos.Habits;
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
        public async Task<Habit> AddHabitAsync(Habit habit)
        {

            _context.Habits.Add(habit);
            await _context.SaveChangesAsync();
            return habit;

        }

        public async Task<int> GetDailyCompletionCountAsync(Guid workerId, HabitType type, DateTime date)
        {
            return await _context.Habits
                .CountAsync(h => h.WorkerId == workerId && h.Type == type && h.CompletedAt! == date.Date);
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

        public async Task<IEnumerable<Habit?>> GetHabitsByWorkerIdAsync(List<Guid> workerIds)
        {
            _logger.LogInformation("Getting habits for worker with id");
            try
            {
                return await _context.Habits
                    .Where(h => workerIds.Contains(h.WorkerId!.Value))
                     .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Enumerable.Empty<Habit>();
            }

        }

        public async Task<bool> UpdateHabitAsync(UpdateHabitDto habit)
        {
            var existing = await _context.Habits.FindAsync(habit.Id);
            if (existing == null) return false;

            existing.Type = habit.Type;
            existing.CompletedAt = habit.CompletedAt;
            existing.Notes = habit.Notes;
            existing.Amount = habit.Amount;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteHabitAsync(DeleteHabitDto habitId)
        {
            var habit = await _context.Habits.FindAsync(habitId);
            if (habit == null) return false;

            _context.Habits.Remove(habit);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Habit?> GetHabitsByIdAsync(Guid habitId)
        {
            return await _context.Habits
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == habitId);
        }

        public async Task<IEnumerable<Habit>> GetAllHabit()
        {
            return await _context.Habits.ToListAsync();
        }

        public async Task<bool> MapHabitToWorkerAsync(Guid habitId, Guid workerId)
        {
            var habit = await _context.Habits.FindAsync(habitId);
            if (habit == null)
                return false;

            var worker = await _context.Workers.FindAsync(workerId);
            if (worker == null)
                return false;

            habit.WorkerId = worker.Id;
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
