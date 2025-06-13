using System.ComponentModel.DataAnnotations;

namespace WorkersManagement.Domain.Dtos.Habits
{
    public class DeleteHabitDto
    {
        [Required]
        [Display(Name = "Habit ID")]

        public Guid Id { get; set; }
    }
}
