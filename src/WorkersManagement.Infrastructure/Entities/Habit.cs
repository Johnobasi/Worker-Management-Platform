﻿using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Infrastructure
{
    public class Habit
    {
        public Guid Id { get; set; }
        public Guid? WorkerId { get; set; }
        public HabitType Type { get;  set; }
        public DateTime CompletedAt { get;  set; } = DateTime.UtcNow;
        public string Notes { get;  set; }

        public Worker Worker { get;  set; }
        public decimal? Amount { get; set; } // Amount is optional and applies to 'Giving' type habits
    }
}
