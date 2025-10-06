using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Infrastructure.Configurations
{
    internal class SubTeamConfiguration : IEntityTypeConfiguration<SubTeam>
    {
        public void Configure(EntityTypeBuilder<SubTeam> builder)
        {
           builder.HasKey(st => st.Id);

            builder.Property(st => st.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(st => st.Description)
                .HasMaxLength(500);

            builder.HasOne(st => st.Team)
                .WithMany(d => d.Subteams)
                .HasForeignKey(st => st.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
