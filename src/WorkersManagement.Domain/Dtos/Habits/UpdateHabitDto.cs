using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Dtos.Habits
{
    public class UpdateHabitDto
    {
        public Guid Id { get; set; }
        public HabitType Type { get; set; }
        public DateTime CompletedAt { get; set; }
        public string Notes { get; set; }
        public decimal? Amount { get; set; }
    }
}
