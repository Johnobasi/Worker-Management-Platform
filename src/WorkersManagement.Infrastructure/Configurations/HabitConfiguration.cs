using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Infrastructure.Configurations
{
    public class HabitConfiguration : IEntityTypeConfiguration<Habit>
    {
        public void Configure(EntityTypeBuilder<Habit> builder)
        {
            builder.HasKey(h => h.Id);

            builder.Property(h => h.Type)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(h => h.CompletedAt)
                .IsRequired();

            builder.Property(h => h.Notes)
                .HasMaxLength(1000);

            builder.HasOne(h => h.Worker)
                .WithMany(w => w.Habits)
                .HasForeignKey(h => h.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(h => h.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Property(h => h.GivingType)
                .HasConversion<string>()        // ⭐ THIS IS THE CHANGE
                .HasMaxLength(100)              // good practice
                .IsRequired(false);
        }
    }
}
