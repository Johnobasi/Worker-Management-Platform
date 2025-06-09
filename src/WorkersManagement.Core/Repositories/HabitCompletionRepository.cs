using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Core.Repositories
{
    public class HabitCompletionRepository : IHabitCompletionRepository
    {
        private readonly WorkerDbContext _workerDbContext;
        private readonly ILogger<HabitCompletionRepository> _logger;
        public HabitCompletionRepository(WorkerDbContext workerDbContext, ILogger<HabitCompletionRepository> logger)
        {
            _workerDbContext = workerDbContext;
            _logger = logger;
        }
        public async Task<HabitCompletion> AddCompletionAsync(HabitCompletion completion)
        {
            var habit = await _workerDbContext.Habits
                .FirstOrDefaultAsync(h => h.Id == completion.HabitId && h.WorkerId == completion.WorkerId);

            if (habit == null)
            {
                throw new InvalidOperationException("Habit does not exist or does not belong to the specified worker.");
            }

            var existingCompletion = await _workerDbContext.HabitCompletions
                .FirstOrDefaultAsync(hc => hc.HabitId == completion.HabitId && hc.CompletedAt.Date == completion.CompletedAt.Date);

            if (existingCompletion != null)
            {
                existingCompletion.IsCompleted = completion.IsCompleted;
                existingCompletion.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                completion.Id = Guid.NewGuid();
                completion.CreatedAt = DateTime.UtcNow;
                await _workerDbContext.HabitCompletions.AddAsync(completion);
            }

            await _workerDbContext.SaveChangesAsync();
            return existingCompletion ?? completion;
        }

        public async Task<int> GetCompletionCountByWorkerAndTypeAsync(Guid workerId, HabitType type, DateTime? date = null)
        {
            try
            {
                var query = _workerDbContext.HabitCompletions
                    .Join(_workerDbContext.Habits,
                        hc => hc.HabitId,
                        h => h.Id,
                        (hc, h) => new { HabitCompletion = hc, Habit = h })
                    .Where(hc => hc.Habit.WorkerId == workerId && hc.Habit.Type == type && hc.HabitCompletion.IsCompleted);

                if (date.HasValue)
                {
                    query = query.Where(hc => hc.HabitCompletion.CompletedAt.Date == date.Value.Date);
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving completion count for worker {workerId} and type {type}");
                return 0;
            }
        }

        public async Task<List<HabitCompletion>> GetCompletionsByWorkerAndTypeAsync(Guid workerId, HabitType type)
        {
            try
            {
                return await _workerDbContext.HabitCompletions
                    .Join(_workerDbContext.Habits,
                        completion => completion.HabitId,
                        habit => habit.Id,
                        (completion, habit) => new { Completion = completion, Habit = habit })
                    .Where(joined => joined.Habit.WorkerId == workerId && joined.Habit.Type == type && joined.Completion.IsCompleted)
                    .Select(joined => joined.Completion)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving completions for worker {workerId} and type {type}");
                return new List<HabitCompletion>();
            }
        }
    }
}
