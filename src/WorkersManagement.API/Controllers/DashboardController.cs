using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController(
        IHabitRepository habitRepository,
        IWorkerManagementRepository workerRepository,
        IAttendanceRepository attendanceRepository,
        IWorkerRewardRepository workerRewardRepository,
        ILogger<DashboardController> logger,
        IHabitCompletionRepository habitCompletionRepository) : ControllerBase
    {
        private readonly IHabitRepository _habitRepository = habitRepository;
        private readonly IWorkerManagementRepository _workerRepository = workerRepository;
        private readonly IAttendanceRepository _attendanceRepository = attendanceRepository;
        private readonly IWorkerRewardRepository _workerRewardRepository = workerRewardRepository;
        private readonly ILogger<DashboardController> _logger = logger;
        private readonly IHabitCompletionRepository _habitCompletionRepository = habitCompletionRepository;

        [HttpGet("{workerId}")]
        public async Task<IActionResult> GetDashboard(Guid workerId)
        {
            try
            {
                var currentWorkerId = User.FindFirst("WorkerId")?.Value;
                var isAdmin = User.IsInRole(UserRole.Admin.ToString());

                // Only Admins can view other workers' dashboards
                if (!isAdmin && workerId.ToString() != currentWorkerId)
                    return Forbid("Workers can only view their own dashboard.");

                var today = DateTime.UtcNow.Date;

                // Daily completions
                var givingToday = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.Giving, today);
                var fastingToday = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.Fasting, today);
                var bibleStudyToday = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.BibleStudy, today);
                var nlpPrayerToday = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.NLPPrayer, today);
                var devotionalToday = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.Devotionals, today);

                // Total completions
                var givingTotal = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.Giving);
                var fastingTotal = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.Fasting);
                var bibleStudyTotal = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.BibleStudy);
                var nlpPrayerTotal = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.NLPPrayer);
                var devotionalTotal = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.Devotionals);

                var givingHabits = await _habitRepository.GetHabitsByTypeAsync(workerId, HabitType.Giving) ?? new List<Habit>();
                var worker = await _workerRepository.GetWorkerByIdAsync(workerId);
                var attendances = await _attendanceRepository.GetWorkerAttendances(workerId, today);
                var workerReward = await _workerRewardRepository.GetRewardsForWorkerAsync(workerId);

                var totalGivingAmount = givingHabits.Sum(h => h.Amount ?? 0);

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
                    HabitSummary = new
                    {
                        Giving = new
                        {
                            TodayCount = givingToday,
                            TotalCount = givingTotal,
                            TotalAmount = totalGivingAmount
                        },
                        Fasting = new
                        {
                            TodayCount = fastingToday,
                            TotalCount = fastingTotal
                        },
                        BibleStudy = new
                        {
                            TodayCount = bibleStudyToday,
                            TotalCount = bibleStudyTotal
                        },
                        NLPPrayer = new
                        {
                            TodayCount = nlpPrayerToday,
                            TotalCount = nlpPrayerTotal
                        },
                        Devotionals = new
                        {
                            TodayCount = devotionalToday,
                            TotalCount = devotionalTotal
                        }
                    }
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving dashboard.");
                return StatusCode(500, "An error occurred while retrieving the dashboard.");
            }
        }
    }
}
