using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;
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

        public async Task<Habit> AddHabitAsync(Habit habit, Guid loggedInWorkerId)
        {

            try
            {
                if (!Enum.IsDefined(typeof(HabitType), habit.Type))
                    throw new ArgumentException("Invalid habit type");

                habit.WorkerId = loggedInWorkerId;

                // If giving, ensure giving type is provided
                if (habit.Type == HabitType.Giving && !habit.GivingType.HasValue)
                {
                    throw new ArgumentException("Giving type must be specified for Giving habit.");
                }

                var workerExists = await _context.Workers
                    .AnyAsync(w => w.Id == loggedInWorkerId);

                var request = new Habit
                {
                    Id = Guid.NewGuid(),
                    WorkerId = loggedInWorkerId,
                    Type = habit.Type,
                    Notes = habit.Notes,
                    Amount = habit.Amount,
                    GivingType = habit.GivingType, // store the selected giving type
                    CompletedAt = DateTime.UtcNow
                };
                _context.Habits.Add(request);
                await _context.SaveChangesAsync();
                return habit;
            }
            catch (DbException ex)
            {
               _logger.LogError(ex, "Database error occurred while adding habit.");
                return new Habit();
            }
        }

        public async Task<int> GetDailyCompletionCountAsync(Guid workerId, HabitType type, DateTime date)
        {
            try
            {
                return await _context.Habits
                    .CountAsync(h => h.WorkerId == workerId 
                    && h.Type == type 
                    && h.CompletedAt! == date.Date);
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Database error occurred while counting daily habit completions.");
                return 0;
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
            try
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
            catch (DbException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating habit.");
                return false;
            }
        }

        public async Task LogHabitAsync(Guid workerId, HabitType habitType, string? notes = null)
        {
            try
            {
                var habit = new Habit
                {
                    Id = Guid.NewGuid(),
                    WorkerId = workerId,
                    Type = habitType,
                    CompletedAt = DateTime.UtcNow,
                    Notes = notes
                };

                await _context.Habits.AddAsync(habit);
                await _context.SaveChangesAsync();
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Database error occurred while logging habit.");
            }

        }
        public async Task<bool> DeleteHabitAsync(Guid habitId)
        {
            try
            {
                var habit = await _context.Habits.FindAsync(habitId);
                if (habit == null)

                    return false;

                _context.Habits.Remove(habit);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Database error occurred while deleting habit.");
                return false;
            }

        }

        public async Task<Habit?> GetHabitsByIdAsync(Guid habitId)
        {
            try
            {
                return await _context.Habits
                  .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == habitId);
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Database error occurred while retrieving habit by ID.");
                return new Habit();
            }

        }

        public async Task<IEnumerable<Habit>> GetAllHabit()
        {
            try
            {
                return await _context.Habits
                    .AsNoTracking()
                        .ToListAsync();
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Database error occurred while retrieving all habits.");
                return Enumerable.Empty<Habit>();
            }

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

        public async Task<List<Habit>> GetHabitsByWorkerAsync(Guid workerId)
        {
            try
            {
                return await _context.Habits
                    .Where(h => h.WorkerId == workerId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving habits for worker {workerId}");
                return new List<Habit>();
            }
        }

    }
}
