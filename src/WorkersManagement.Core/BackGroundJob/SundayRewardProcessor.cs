using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;

namespace WorkersManagement.Core.BackGroundJob
{
    public class SundayRewardProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SundayRewardProcessor> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

        public SundayRewardProcessor(
            IServiceScopeFactory scopeFactory,
            ILogger<SundayRewardProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var workerRewardRepository = scope.ServiceProvider.GetRequiredService<IWorkerRewardRepository>();
                var context = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();

                var currentTime = DateTime.Now;

                if (workerRewardRepository.IsSunday(currentTime) && IsScheduledTime(currentTime))
                {
                    var activeWorkers = await context.Workers
                        .Where(w => w.Status.HasValue)
                        .ToListAsync(stoppingToken);

                    foreach (var worker in activeWorkers)
                    {
                        try
                        {
                            await workerRewardRepository.ProcessSundayAttendance(worker.Id, currentTime);
                            _logger.LogInformation($"Successfully processed attendance for worker: {worker.Id}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Failed to process attendance for worker {worker.Id}: {ex.Message}");
                        }
                    }
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private static bool IsScheduledTime(DateTime currentTime)
        {
            // Run at 10:00 AM UTC on Sundays
            return currentTime.Hour == 14 && currentTime.Minute == 0;
        }
    }
}
