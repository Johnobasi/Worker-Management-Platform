using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;

namespace WorkersManagement.Core.BackGroundJob
{
    public class SundayRewardProcessor : IJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SundayRewardProcessor> _logger;

        public SundayRewardProcessor(IServiceScopeFactory scopeFactory, ILogger<SundayRewardProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Starting Sunday reward processing...");

            using var scope = _scopeFactory.CreateScope();
            var rewardRepo = scope.ServiceProvider.GetRequiredService<IWorkerRewardRepository>();
            var dbContext = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();

            // Get all worker IDs
            var workerIds = await dbContext.Workers.Select(w => w.Id).ToListAsync();

            foreach (var workerId in workerIds)
            {
                try
                {
                    await rewardRepo.CheckAndProcessReward(workerId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process reward for worker {workerId}");
                }
            }

            _logger.LogInformation("Sunday reward processing completed.");
        }
    }
}
