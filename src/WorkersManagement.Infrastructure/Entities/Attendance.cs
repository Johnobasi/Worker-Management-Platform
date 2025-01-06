using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Infrastructure
{
    public class Attendance
    {
        public Guid Id { get; set; }
        public Guid WorkerId { get; set; }
        public DateTime CheckInTime { get; set; }
        public AttendanceType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public AttendanceStatus Status { get; set; }
        public bool IsEarlyCheckIn { get; set; }
        public Worker Worker { get;  set; }
    }
}
