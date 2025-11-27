using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Infrastructure
{
    public class Reward
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int PointsRequired { get; set; }
        public bool Status { get; set; }
        public RewardType Type { get; set; }


        public ICollection<WorkerReward> WorkerRewards { get; set; }
    }
}
