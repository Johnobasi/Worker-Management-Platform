using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Infrastructure
{
    public class WorkerReward
    {
        public Guid RewardId { get; set; }
        public DateTime EarnedAt { get;  set; }
        public Reward Reward { get; set; }
        public Guid Id { get; set; }
        public Guid WorkerId { get; set; }
        public RewardType RewardType { get; set; }
        public RewardStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RedeemedAt { get; set; }

        public Worker Worker { get; set; }
    }

}
