using Microsoft.EntityFrameworkCore;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Infrastructure;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IWorkerRewardRepository
    {
        Task ProcessSundayAttendance(Guid workerId, DateTime checkInTime);
        Task SaveReward(WorkerReward reward);
        Task ResetConsecutiveCount(Guid workerId);
        bool IsSunday(DateTime date);
        bool IsEarlyCheckIn(DateTime checkInTime);
        Task<WorkerRewardResponse> GetRewardsForWorkerAsync(Guid workerId);
    }
}
