using System.ComponentModel.DataAnnotations;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Dtos
{
    public class AddHabitRequest
    {
        [Required]
        public HabitType Type { get; set; }

        [Required(ErrorMessage = "Habit name is required.")]

        public string Notes { get; set; }


        public decimal? Amount { get; set; } // Only required for Giving
    }
}
