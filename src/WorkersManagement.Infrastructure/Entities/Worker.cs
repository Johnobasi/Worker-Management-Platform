using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Infrastructure
{
    public class Worker
    {
        public Guid Id { get; set; }
        public List<UserRole> Roles { get; set; } = new();
        public List<WorkerType> Type { get; set; }
        public string WorkerNumber { get; set; }
        public string FirstName { get;  set; }
        public string LastName { get;  set; }
        public string ProfilePictureUrl { get; set; } = null;
        public string QRCode { get; set; }
        public bool? Status { get; set; } 
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime? LastLogin { get; set; }
        public string PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiration { get; set; }
        public DateTime? LastRewardDate { get; set; }
        public Department Department { get; set; }
        public ICollection<Attendance> Attendances { get; set; }
        public ICollection<Habit> Habits { get; set; }
        public ICollection<HabitCompletion> HabitCompletions { get; set; } = new List<HabitCompletion>();
        public ICollection<WorkerReward> Rewards { get; set; }
        public Guid? DepartmentId { get; set; } // Explicit foreign key
        public bool IsConfirmed { get; set; }
    }

}
