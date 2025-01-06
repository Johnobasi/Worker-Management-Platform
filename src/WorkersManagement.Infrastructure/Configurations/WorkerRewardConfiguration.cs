using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkersManagement.Infrastructure;

public class WorkerRewardConfiguration : IEntityTypeConfiguration<WorkerReward>
{
    public void Configure(EntityTypeBuilder<WorkerReward> builder)
    {
        builder.HasKey(wr => wr.Id);

        builder.Property(wr => wr.EarnedAt)
            .IsRequired();

        builder.Property(wr => wr.RedeemedAt)
            .IsRequired(false);

        builder.HasOne(wr => wr.Worker)
            .WithMany(w => w.Rewards)
            .HasForeignKey(wr => wr.WorkerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wr => wr.Reward)
            .WithMany()
            .HasForeignKey(wr => wr.RewardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
