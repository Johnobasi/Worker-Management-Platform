using System.ComponentModel.DataAnnotations;

public class MapHabitToWorkerDto
{
    [Required(ErrorMessage = "Habit ID is required.")]
    public Guid HabitId { get; set; }

    [Required(ErrorMessage = "Worker ID is required.")]
    public Guid WorkerId { get; set; }
}
