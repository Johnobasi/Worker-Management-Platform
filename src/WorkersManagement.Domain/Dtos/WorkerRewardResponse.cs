using WorkersManagement.Infrastructure;

namespace WorkersManagement.Domain.Dtos
{
    public class WorkerRewardResponse
    {
        public ICollection<WorkerReward> Rewards { get; set; }
        public string Message { get; set; }
    }
}
