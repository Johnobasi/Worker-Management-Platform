using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Infrastructure
{
    public class Worker
    {
        public Guid Id { get; private set; }
        public Guid DepartmentId { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string QRCode { get; private set; }
        public bool Status { get; private set; }

        public int ConsecutiveSundayCount { get; set; }
        public DateTime? LastRewardDate { get; set; }
        public User User { get; private set; }
        public Department Department { get; private set; }
        public ICollection<Attendance> Attendances { get; private set; }
        public ICollection<Habit> Habits { get; private set; }
        public ICollection<WorkerReward> Rewards { get; private set; }
    }

}
