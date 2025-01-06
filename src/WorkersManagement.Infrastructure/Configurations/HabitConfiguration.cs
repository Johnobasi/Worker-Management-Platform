using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
        }
    }
}
