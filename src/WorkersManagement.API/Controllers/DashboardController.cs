using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Dtos.Habits;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
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

                // Group by type
                var habitGroups = allHabits.GroupBy(h => h.Type)
                                           .ToDictionary(g => g.Key, g => g.ToList());

                var habitSummary = habitGroups.ToDictionary(
                    g => g.Key.ToString(),
                    g => new
                    {
                        HabitCount = g.Value.Count, // total habits of this type
                        TodayCount = g.Value.Count(h => h.CompletedAt.Date == today), // completed today
                        TotalCount = g.Value.Count, // all completed habits (all rows in table)
                        TotalAmount = g.Key == HabitType.Giving
                            ? "£" + g.Value.Sum(h => h.Amount ?? 0).ToString("N2") // formatted with 2 decimals
                            : "£0.00"
                    }
                );
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
                    Attendance = attendances.Count(),
                    Reward = workerReward,
                    HabitSummary = habitSummary
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving dashboard.");
                return StatusCode(500, "An error occurred while retrieving the dashboard.");
            }
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
