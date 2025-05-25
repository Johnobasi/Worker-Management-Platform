using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Dtos.Habits;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IHabitRepository
    {
        Task<IEnumerable<Habit>> GetHabitsByWorkerIdAsync(List<Guid> workerIds);
        Task AddHabitAsync(Habit habit);
        Task<IEnumerable<Habit>> GetHabitsByTypeAsync(Guid workerId, HabitType type);
        Task<bool> UpdateHabitAsync(UpdateHabitDto habit);
        Task<bool> DeleteHabitAsync(DeleteHabitDto habitId);
        Task<Habit> GetHabitsByIdAsync(Guid habitId);

        Task<IEnumerable<Habit>> GetAllHabit();
    }
}
