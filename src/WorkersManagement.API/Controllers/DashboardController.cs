using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController(IHabitRepository habitRepository, 
        IWorkerManagementRepository workerRepository, IAttendanceRepository attendanceRepository,
        IWorkerRewardRepository workerRewardRepository, ILogger<DashboardController> logger) : ControllerBase
    {
        private readonly IHabitRepository habitRepository = habitRepository;
        private readonly IWorkerManagementRepository workerRepository = workerRepository;
        private readonly IAttendanceRepository attendanceRepository = attendanceRepository;
        private readonly IWorkerRewardRepository workerRewardRepository = workerRewardRepository;
        private readonly ILogger<DashboardController> logger = logger;

        [HttpGet("{workerId}")]
        public async Task<IActionResult> GetDashboard(Guid workerId)
        {
            try
            {
                var givingHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId, HabitType.Giving) ?? new List<Habit>();
                var fastingHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId, HabitType.Fasting) ?? new List<Habit>();
                var bibleStudyHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId, HabitType.BibleStudy) ?? new List<Habit>();
                var nlpPrayerHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId, HabitType.NLPPrayer) ?? new List<Habit>();
                var devotionalHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId, HabitType.Devotionals) ?? new List<Habit>();
                var worker = await this.workerRepository.GetWorkerByIdAsync(workerId);
                var attendances = await this.attendanceRepository.GetWorkerAttendances(workerId, DateTime.Now.Date);
                var workerReward = await this.workerRewardRepository.GetRewardsForWorkerAsync(workerId);

                // Calculate the total amount for Giving habits
                var totalGivingAmount = givingHabits.Sum(h => h.Amount ?? 0);

                var dashboard = new
                {
                    Worker = new
                    {
                        worker.FirstName,
                        worker.LastName,
                        Department = worker.Department.Name,
                        Team = worker.Department.Teams.Name
                    },
                    Attendance = attendances.Count(),
                    Reward = workerReward,//
                    HabitSummary = new
                    {
                        Giving = new
                        {
                            Count = givingHabits.Count(),
                            TotalAmount = totalGivingAmount
                        },
                        Fasting = fastingHabits.Count(),
                        BibleStudy = bibleStudyHabits.Count(),
                        NLPPrayer = nlpPrayerHabits.Count(),
                        Devotionals = devotionalHabits.Count()
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
