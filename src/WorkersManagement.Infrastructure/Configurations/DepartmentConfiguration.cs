using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkersManagement.Infrastructure.Entities;


namespace WorkersManagement.Infrastructure.Configurations
{
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.HasKey(d => d.Id);

            builder.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(d => d.Description)
                .HasMaxLength(500);

            builder.HasOne(d => d.Teams)
                .WithMany(t => t.Departments)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(d => d.Users)
                .WithOne(w => w.Department)
                .HasForeignKey(w => w.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
