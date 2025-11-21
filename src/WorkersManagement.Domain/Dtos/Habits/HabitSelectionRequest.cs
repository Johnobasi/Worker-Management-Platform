using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Dtos.Habits
{
    public class HabitSelectionRequest
    {
        public Guid WorkerId { get; set; }
        public List<HabitType> SelectedHabits { get; set; } = new();
    }
}
