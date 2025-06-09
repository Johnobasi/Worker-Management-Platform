using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IHabitCompletionRepository
    {
        Task AddCompletionAsync(HabitCompletion completion);
        Task<List<HabitCompletion>> GetCompletionsByWorkerAndTypeAsync(Guid workerId, HabitType type);
        Task<int> GetCompletionCountByWorkerAndTypeAsync(Guid workerId, HabitType type, DateTime? date = null);
    }
}
