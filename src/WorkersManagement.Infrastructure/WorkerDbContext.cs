using Microsoft.EntityFrameworkCore;
using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

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
        public DbSet<HabitCompletion> HabitCompletions { get; set; }
        public DbSet<SubTeam> SubTeams { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkerDbContext).Assembly);

            modelBuilder.Entity<Worker>()
                .Property(w => w.Role)
                .HasConversion(
                    v => v.ToString(), // Convert enum to string
                    v => Enum.Parse<UserRole>(v));
           
            modelBuilder.Entity<Worker>()
            .HasMany(w => w.Habits)
            .WithOne(h => h.Worker)
            .HasForeignKey(h => h.WorkerId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Habit>()
                .HasMany(h => h.Completions)
                .WithOne(c => c.Habit)
                .HasForeignKey(c => c.HabitId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Worker>()
            .HasOne(w => w.Department)
            .WithMany(d => d.Workers)
            .HasForeignKey(w => w.DepartmentId)
            .IsRequired(false);
            }
    }
}
