using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Dtos.Habits
{
    public class WorkerHabitPreferenceDto
    {
        public HabitType Type { get; set; }
        public string Notes { get; set; }
    }

    public class UpdateWorkerHabitPreferencesDto
    {
        public ICollection<WorkerHabitPreferenceDto> Habits { get; set; } = [];
    }
}