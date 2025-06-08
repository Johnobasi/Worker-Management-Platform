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
        public async Task AddCompletionAsync(UpdateHabit completion)
        {
            await _workerDbContext.HabitUpdates.AddAsync(completion);
            await _workerDbContext.SaveChangesAsync();
        }

        public async Task<int> GetCompletionCountByWorkerAndTypeAsync(Guid workerId, HabitType type, DateTime? date = null)
        {
            var query = _workerDbContext.HabitUpdates
                 .Where(c => c.WorkerId == workerId && c.Type == type);

            if (date.HasValue)
            {
                query = query.Where(c => c.CompletedAt.Date == date.Value.Date);
            }

            return await query.CountAsync();
        }

        public async Task<List<UpdateHabit>> GetCompletionsByWorkerAndTypeAsync(Guid workerId, HabitType type)
        {
            return await _workerDbContext.HabitUpdates
                 .Where(c => c.WorkerId == workerId && c.Type == type)
                 .ToListAsync();
        }
    }
}
