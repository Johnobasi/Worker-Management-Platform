﻿using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Infrastructure
{
    public class Worker
    {
        public Guid Id { get; set; }
        public Guid DepartmentId { get; set; }
        public UserRole Role { get; set; }
        public string WorkerNumber { get; set; }
        public string FirstName { get;  set; }
        public string LastName { get;  set; }
        public string QRCode { get; set; }
        public bool Status { get; set; }
        public string Email { get; set; }
        public int ConsecutiveSundayCount { get; set; }
        public DateTime? LastRewardDate { get; set; }
        public Department Department { get; set; }
        public ICollection<Attendance> Attendances { get; set; }
        public ICollection<Habit> Habits { get; set; }
        public ICollection<WorkerReward> Rewards { get; set; }
    }

}
