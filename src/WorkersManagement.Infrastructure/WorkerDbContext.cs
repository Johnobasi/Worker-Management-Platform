using Microsoft.EntityFrameworkCore;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Infrastructure
{
    public class WorkerDbContext(DbContextOptions<WorkerDbContext> options) : DbContext(options)
    {
        public DbSet<Worker> Workers { get; set; }
        public DbSet<WorkerReward> WorkerRewards { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Habit> Habits { get; set; }
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<QRCode> QRCodes { get; set; }
        public DbSet<Devotional> Devotionals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkerDbContext).Assembly);
        }
    }
}
