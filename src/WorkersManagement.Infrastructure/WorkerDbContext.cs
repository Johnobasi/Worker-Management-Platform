using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<HabitCompletion>()
                .HasKey(hc => hc.Id);

            modelBuilder.Entity<HabitCompletion>()
                .Property(hc => hc.IsCompleted)
                .IsRequired();

            modelBuilder.Entity<HabitCompletion>()
                .Property(hc => hc.WorkerId)
                .IsRequired();

            modelBuilder.Entity<HabitCompletion>()
                .Property(hc => hc.HabitId)
                .IsRequired();

            modelBuilder.Entity<HabitCompletion>()
                .Property(hc => hc.Type)
                .HasConversion(
                    v => v.ToString(),
                    v => (HabitType)Enum.Parse(typeof(HabitType), v))
                .IsRequired();

            modelBuilder.Entity<HabitCompletion>()
                .Property(hc => hc.Notes)
                .HasMaxLength(500);

            modelBuilder.Entity<HabitCompletion>()
                .HasOne(hc => hc.Habit)
                .WithMany(h => h.Completions)
                .HasForeignKey(hc => hc.HabitId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HabitCompletion>()
                .HasOne(hc => hc.Worker)
                .WithMany(w => w.HabitCompletions)
                .HasForeignKey(hc => hc.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Habit>()
                .Property(h => h.Type)
                .HasConversion(
                    v => v.ToString(),
                    v => (HabitType)Enum.Parse(typeof(HabitType), v));

                modelBuilder.Entity<Worker>()
                .Property(w => w.Roles)
                .HasConversion(new UserRoleListConverter());

            modelBuilder.Entity<Worker>()
                .HasMany(w => w.Habits)
                .WithOne(h => h.Worker)
                .HasForeignKey(h => h.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Worker>()
                .HasOne(w => w.Department)
                .WithMany(d => d.Workers)
                .HasForeignKey(w => w.DepartmentId)
                .IsRequired(false);


            modelBuilder.Entity<HabitCompletion>()
                .HasOne(hc => hc.Worker)
                .WithMany()
                .HasForeignKey(hc => hc.WorkerId)
                .OnDelete(DeleteBehavior.NoAction);



        }

        public class UserRoleListConverter : ValueConverter<List<UserRole>, string>
        {
            public UserRoleListConverter()
                : base(
                    v => string.Join(",", v.Select(r => r.ToString())),
                    v => v.Split(",", StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => (UserRole)Enum.Parse(typeof(UserRole), s))
                          .ToList()
                )
            { }
        }
    }
}
