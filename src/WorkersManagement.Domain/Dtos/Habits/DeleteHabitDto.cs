using System.ComponentModel.DataAnnotations;

namespace WorkersManagement.Domain.Dtos.Habits
{
    public class DeleteHabitDto
    {
        [Required]
        [Display(Name = "Habit ID")]

        public Guid Id { get; set; }
    }

    public class AttendanceDto
    {
        public Guid Id { get; set; }
        public Guid WorkerId { get; set; }
        public DateTime CheckInTime { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsEarlyCheckIn { get; set; }
        public WorkerDto Worker { get; set; } = new WorkerDto();
    }

    public class WorkerDto
    {
        public Guid Id { get; set; }
        public string WorkerNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
    }
}
