using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Infrastructure
{
    public class Habit
    {
        public Guid Id { get; set; }
        public Guid? WorkerId { get; set; }
        public HabitType Type { get;  set; }
        public DateTime CompletedAt { get;  set; }
        public string Notes { get;  set; }

        public Worker Worker { get;  set; }
        public decimal? Amount { get; set; } // Amount is optional and applies to 'Giving' type habits
        public ICollection<HabitCompletion> Completions { get; set; } = new List<HabitCompletion>();
        public GivingType? GivingType { get; set; } // only used if Type == Giving
    }

    public enum GivingType
    {
        Tithe,
        Offering,
        FoodBank,
        OtherKingdomDonations
    }
}
