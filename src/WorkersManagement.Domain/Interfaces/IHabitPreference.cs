using WorkersManagement.Domain.Dtos.Habits;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IHabitPreference
    {
        Task SaveHabitsAsync(HabitSelectionRequest request);
        Task<DashboardResponse> GetDashboardForWorkerAsync(Guid workerId);

        /// <summary>
        /// Updates an existing worker's habit preferences.
        /// </summary>
        Task UpdateHabitPreferencesAsync(Guid workerId, UpdateHabitPreferencesRequest request);
    }

    public class UpdateHabitPreferencesRequest
    {
        public ICollection<WorkerHabitPreferenceDto> Habits { get; set; } =  [];
    }
}
