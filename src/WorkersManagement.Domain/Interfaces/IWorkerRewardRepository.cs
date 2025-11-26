using WorkersManagement.Domain.Dtos;
using WorkersManagement.Infrastructure;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IWorkerRewardRepository
    {
        Task SaveReward(WorkerReward reward);
        Task ResetConsecutiveCount(Guid workerId);
        Task<WorkerRewardResponse> GetRewardsForWorkerAsync(Guid workerId);

        Task CheckAndProcessReward(Guid workerId);
        Task ProcessAllRewardsAsync();
    }
}
