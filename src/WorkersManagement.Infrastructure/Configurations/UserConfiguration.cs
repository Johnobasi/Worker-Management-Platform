using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WorkersManagement.Infrastructure.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.PasswordHash)
                .IsRequired();

            builder.Property(u => u.Role)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Correct relationship: User belongs to one Department
            builder.HasOne(u => u.Department)
                .WithMany(d => d.Users) // Department has many Users
                .HasForeignKey(u => u.DepartmentId) // Foreign key is DepartmentId in User
                .OnDelete(DeleteBehavior.SetNull); // Set DepartmentId to NULL if the Department is deleted
        }
    }
}
