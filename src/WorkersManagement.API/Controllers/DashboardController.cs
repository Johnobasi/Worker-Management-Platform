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
    public class DashboardController(IHabitRepository habitRepository, 
        IWorkerManagementRepository workerRepository, IAttendanceRepository attendanceRepository,
        IWorkerRewardRepository workerRewardRepository, ILogger<DashboardController> logger, IHabitCompletionRepository habitCompletionRepository) : ControllerBase
    {
        private readonly IHabitRepository habitRepository = habitRepository;
        private readonly IWorkerManagementRepository workerRepository = workerRepository;
        private readonly IAttendanceRepository attendanceRepository = attendanceRepository;
        private readonly IWorkerRewardRepository workerRewardRepository = workerRewardRepository;
        private readonly ILogger<DashboardController> logger = logger;
        private readonly IHabitCompletionRepository _habitCompletionRepository = habitCompletionRepository;

        [HttpGet("{workerId}")]
        [Authorize(Policy = "Worker")]
        public async Task<IActionResult> GetDashboard(Guid workerId)
        {
            try
            {
                // Workers can only view their own dashboard
                if (User.IsInRole(UserRole.Worker.ToString()) && workerId.ToString() != User.FindFirst("WorkerId")?.Value)
                    return Forbid("Workers can only view their own dashboard.");

                // Get today's date for daily counts
                var today = DateTime.UtcNow.Date;

                // Get completion counts for today
                var givingCompletionsToday = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.Giving, today);
                var fastingCompletionsToday = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.Fasting, today);
                var bibleStudyCompletionsToday = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.BibleStudy, today);
                var nlpPrayerCompletionsToday = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.NLPPrayer, today);
                var devotionalCompletionsToday = await this._habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.Devotionals, today);

                //var givingHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId, HabitType.Giving) ?? new List<Habit>();
                //var fastingHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId, HabitType.Fasting) ?? new List<Habit>();
                //var bibleStudyHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId, HabitType.BibleStudy) ?? new List<Habit>();
                //var nlpPrayerHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId, HabitType.NLPPrayer) ?? new List<Habit>();
                //var devotionalHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId, HabitType.Devotionals) ?? new List<Habit>();
                //var worker = await this.workerRepository.GetWorkerByIdAsync(workerId);
                //var attendances = await this.attendanceRepository.GetWorkerAttendances(workerId, DateTime.Now.Date);
                //var workerReward = await this.workerRewardRepository.GetRewardsForWorkerAsync(workerId);

                // Get total completion counts
                var givingCompletionsTotal = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.Giving);
                var fastingCompletionsTotal = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.Fasting);
                var bibleStudyCompletionsTotal = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.BibleStudy);
                var nlpPrayerCompletionsTotal = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.NLPPrayer);
                var devotionalCompletionsTotal = await _habitCompletionRepository.GetCompletionCountByWorkerAndTypeAsync(workerId, HabitType.Devotionals);

                var givingHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId, HabitType.Giving) ?? new List<Habit>();
                var worker = await this.workerRepository.GetWorkerByIdAsync(workerId);
                var attendances = await this.attendanceRepository.GetWorkerAttendances(workerId, today);
                var workerReward = await this.workerRewardRepository.GetRewardsForWorkerAsync(workerId);

                // Calculate the total amount for Giving habits
                var totalGivingAmount = givingHabits.Sum(h => h.Amount ?? 0);

                var dashboard = new
                {
                    Worker = new
                    {
                        worker.FirstName,
                        Id = worker.Id,
                        worker.LastName,
                        Department = worker.Department.Name,
                        Team = worker.Department.Teams.Name
                    },
                    Attendance = attendances.Count(),
                    Reward = workerReward,
                    HabitSummary = new
                    {
                        Giving = new
                        {
                            Count = givingCompletionsToday,
                            TotalCount = givingCompletionsTotal,
                            TotalAmount = totalGivingAmount
                        },
                        Fasting = new
                        {
                            TodayCount = fastingCompletionsToday,
                            TotalCount = fastingCompletionsTotal
                        },
                        BibleStudy = new
                        {
                            TodayCount = bibleStudyCompletionsToday,
                            TotalCount = bibleStudyCompletionsTotal
                        },
                        NLPPrayer = new
                        {
                            TodayCount = nlpPrayerCompletionsToday,
                            TotalCount = nlpPrayerCompletionsTotal
                        },
                        Devotionals = new
                        {
                            TodayCount = devotionalCompletionsToday,
                            TotalCount = devotionalCompletionsTotal
                        }
                    }
                };
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving all workers habit.");
                return StatusCode(500, "An error occurred while retrieving the workers habit summary.");
            }
        }
    }
}
