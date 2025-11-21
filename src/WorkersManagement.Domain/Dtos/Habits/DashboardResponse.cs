using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Dtos.Habits
{
    public class DashboardResponse
    {
        public Guid WorkerId { get; set; }
        public string FirstName { get; set; }
        public ICollection<HabitDashboardItem> Habits { get; set; } = [];
    }

    public class HabitDashboardItem
    {
        public HabitType Habit { get; set; }
        public int MonthlyCount { get; set; }
        public decimal MonthlyAmount { get; set; }
        public int AllTimeCount { get; set; }
        public decimal AllTimeAmount { get; set; }
        public int Streak { get; set; }
        public string Message { get; set; }
    }
}
