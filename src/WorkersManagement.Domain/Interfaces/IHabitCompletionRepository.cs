using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IHabitCompletionRepository
    {
        Task<HabitCompletion> AddCompletionAsync(HabitCompletion completion);
        Task<int> GetCompletionCountByWorkerAndTypeAsync(Guid workerId, HabitType type, DateTime? date = null);
        Task<List<HabitCompletion>> GetCompletionsByWorkerAndTypeAsync(Guid workerId, HabitType type);
        Task<Dictionary<HabitType, int>> GetHabitCountsByWorkerAsync(Guid workerId);
    }
}
