using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Infrastructure.Entities
{
    public class WorkerHabitPreference
    {
        public Guid Id { get; set; }
        public Guid WorkerId { get; set; }
        public HabitType HabitType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Worker Worker { get; set; }
    }
}
