using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Infrastructure.Entities
{
   public class HabitCompletion
    {
        public Guid Id { get; set; }
        public Guid HabitId { get; set; }
        public DateTime CompletedAt { get; set; }
        public HabitType Type { get; set; }
        public string Notes { get; set; }
        public Habit Habit { get; set; }
        public Worker Worker { get; set; }
    }

}
