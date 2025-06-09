using Microsoft.EntityFrameworkCore;
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
        public HabitCompletionRepository(WorkerDbContext workerDbContext)
        {
            _workerDbContext = workerDbContext;
        }
        public async Task AddCompletionAsync(HabitCompletion completion)
        {
            // Validate that the habit exists and belongs to the worker
            var habit = await _workerDbContext.Habits
                .FirstOrDefaultAsync(h => h.Id == completion.HabitId);

            if (habit == null)
            {
                throw new InvalidOperationException("Habit does not exist.");
            }

            await _workerDbContext.HabitCompletions.AddAsync(completion);
            await _workerDbContext.SaveChangesAsync();
        }

        public async Task<int> GetCompletionCountByWorkerAndTypeAsync(Guid workerId, HabitType type, DateTime? date = null)
        {
            var query = _workerDbContext.HabitCompletions
                .Join(_workerDbContext.Habits,
                    completion => completion.HabitId,
                    habit => habit.Id,
                    (completion, habit) => new { Completion = completion, Habit = habit })
                .Where(joined => joined.Habit.WorkerId == workerId && joined.Habit.Type == type);

            if (date.HasValue)
            {
                query = query.Where(joined => joined.Completion.CompletedAt.Date == date.Value.Date);
            }

            return await query.CountAsync();
        }

        public async Task<List<HabitCompletion>> GetCompletionsByWorkerAndTypeAsync(Guid workerId, HabitType type)
        {
            return await _workerDbContext.HabitCompletions
                  .Join(_workerDbContext.Habits,
                      completion => completion.HabitId,
                      habit => habit.Id,
                      (completion, habit) => new { Completion = completion, Habit = habit })
                  .Where(joined => joined.Habit.WorkerId == workerId && joined.Habit.Type == type)
                  .Select(joined => joined.Completion)
                  .ToListAsync();
        }
    }
}
