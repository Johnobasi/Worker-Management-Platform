using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WorkersManagement.Infrastructure.Configurations
{
    public class RewardConfiguration : IEntityTypeConfiguration<Reward>
    {
        public void Configure(EntityTypeBuilder<Reward> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(r => r.Description)
                .HasMaxLength(500);

            builder.Property(r => r.PointsRequired)
                .IsRequired();

            builder.HasMany(r => r.WorkerRewards)
                .WithOne(wr => wr.Reward)
                .HasForeignKey(wr => wr.RewardId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
