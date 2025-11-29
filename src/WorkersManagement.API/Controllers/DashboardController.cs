using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Dtos.Habits;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.API.Controllers
{
    /// <summary>
    /// Dashboard data for worker statistics and habits
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController(
        IHabitRepository habitRepository,
        IWorkerManagementRepository workerRepository,
        IAttendanceRepository attendanceRepository,
        IWorkerRewardRepository workerRewardRepository,
        ILogger<DashboardController> logger,
        IHabitCompletionRepository habitCompletionRepository,
        IHabitPreference habitPreference) : ControllerBase
    {
        private readonly IHabitRepository _habitRepository = habitRepository;
        private readonly IWorkerManagementRepository _workerRepository = workerRepository;
        private readonly IAttendanceRepository _attendanceRepository = attendanceRepository;
        private readonly IWorkerRewardRepository _workerRewardRepository = workerRewardRepository;
        private readonly ILogger<DashboardController> _logger = logger;
        private readonly IHabitCompletionRepository _habitCompletionRepository = habitCompletionRepository;
        private readonly IHabitPreference _service = habitPreference;

        /// <summary>
        /// Get worker dashboard with statistics and habit progress
        /// </summary>
        /// <returns>Dashboard data with habits, attendance, and rewards</returns>
        [HttpGet("{workerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                // Get worker ID from claims
                var workerIdClaim = User.FindFirst("workerId")?.Value;
                if (!Guid.TryParse(workerIdClaim, out Guid workerId))
                    return Unauthorized("Invalid worker ID.");

                var today = DateTime.UtcNow.Date;
                // Fetch all habits for the worker
                var allHabits = await _habitRepository.GetHabitsByWorkerAsync(workerId);
                var habitSummary = GetHabitSummary(allHabits);

                var habitSummaryDto = habitSummary.ToDictionary(
                        h => h.Key.ToString(),
                        h => new
                        {
                            TotalCount = h.Value.Count,
                            TotalAmount = "£" + h.Value.TotalAmount.ToString("N2"),
                            TodayCount = allHabits.Count(x => x.Type == h.Key && x.CompletedAt.Date == today)
                        }
                    );

                // Special: Handle Giving types
                var givingDetails = new List<object>();
                if (habitSummary.ContainsKey(HabitType.Giving))
                {
                    var givingHabits = allHabits.Where(h => h.Type == HabitType.Giving).ToList();
                    var givingByType = givingHabits
                        .Where(h => h.GivingType.HasValue)
                        .GroupBy(h => h.GivingType!.Value)
                        .ToDictionary(g => g.Key, g => g.Sum(h => h.Amount ?? 0));

                    foreach (var kvp in givingByType)
                    {
                        givingDetails.Add(new
                        {
                            Type = kvp.Key.ToString(),
                            Amount = "£" + kvp.Value.ToString("N2")
                        });
                    }

                    givingDetails.Add(new
                    {
                        Type = "Total Giving",
                        Amount = "£" + givingByType.Values.Sum().ToString("N2")
                    });
                }
                // Worker info
                var worker = await _workerRepository.GetWorkerByIdAsync(workerId);
                    
                // Attendance
                var attendances = await _attendanceRepository.GetWorkerAttendances(workerId, today);

                // Rewards
                var workerReward = await _workerRewardRepository.GetRewardsForWorkerAsync(workerId);

                // Build dashboard response
                var dashboard = new
                {
                    Worker = new
                    {
                        worker.FirstName,
                        worker.LastName,
                        worker.Id,
                        Department = worker.Department?.Name
                    },
                    Attendance = attendances.SummaryMessages.Count(),
                    Reward = workerReward,
                    HabitSummary = habitSummaryDto
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving dashboard.");
                return StatusCode(500, "An error occurred while retrieving the dashboard.");
            }
        }

        private Dictionary<HabitType, (int Count, decimal TotalAmount)> GetHabitSummary(List<Habit> habits)
        {
            return habits
                .GroupBy(h => h.Type)
                .ToDictionary(
                    g => g.Key,
                    g => (
                        Count: g.Count(),
                        TotalAmount: g.Sum(h => h.Amount ?? 0)
                    )
                );
        }

        /// <summary>
        /// Retrieves the habit-based dashboard for a specific worker.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="DashboardResponse"/> containing the worker's
        /// personalized dashboard based on their selected habit preferences.
        /// </returns>
        /// <response code="200">Dashboard successfully retrieved.</response>
        /// <response code="404">Worker not found.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("worker/preffereddashboard")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPrefferedHabitDashboards()
        {
            var loggeInWorker = User.FindFirst("workerId")?.Value;
            if (!Guid.TryParse(loggeInWorker, out Guid workerId))
                return Unauthorized("Invalid worker ID.");

            var dashboard = await _service.GetDashboardForWorkerAsync(workerId);

            if (dashboard == null)
                return NotFound($"Dashboard not found for worker with ID: {workerId}");

            return Ok(dashboard);
        }

    }
}
