using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Dtos
{
    public class AddHabitRequest
    {
        public Guid WorkerId { get; set; }
        public HabitType Type { get; set; }
        public string Notes { get; set; }
        public decimal? Amount { get; set; }
    }
}
