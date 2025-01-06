namespace WorkersManagement.Infrastructure
{
    public class Reward
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public int PointsRequired { get; private set; }
        public bool Status { get; private set; }

        public ICollection<WorkerReward> WorkerRewards { get; private set; }
    }

}
