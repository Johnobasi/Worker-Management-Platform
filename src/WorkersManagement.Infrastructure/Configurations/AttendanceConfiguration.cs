using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WorkersManagement.Infrastructure.Configurations
{
    public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
    {
        public void Configure(EntityTypeBuilder<Attendance> builder)
        {
            builder.HasKey(a => a.Id);
            
            builder.Property(a => a.CheckInTime)
                .IsRequired();

            builder.Property(a => a.Type)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(a => a.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(a => a.Worker)
                .WithMany(w => w.Attendances)
                .HasForeignKey(a => a.WorkerId);
        }
    }
}
