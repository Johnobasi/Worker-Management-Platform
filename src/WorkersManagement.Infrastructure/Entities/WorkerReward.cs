using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Infrastructure
{
    public class WorkerReward
    {
        public Guid RewardId { get; private set; }
        public DateTime EarnedAt { get; private set; }
        public Reward Reward { get; private set; }
        public Guid Id { get; set; }
        public Guid WorkerId { get; set; }
        public RewardType RewardType { get; set; }
        public RewardStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RedeemedAt { get; set; }

        public Worker Worker { get; set; }
    }

}
