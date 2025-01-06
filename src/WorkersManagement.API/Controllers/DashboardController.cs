using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController(IHabitRepository habitRepository, 
        IUserRepository workerRepository, IAttendanceRepository attendanceRepository,IWorkerRewardRepository workerRewardRepository) : ControllerBase
    {
        private readonly IHabitRepository habitRepository = habitRepository;
        private readonly IUserRepository workerRepository = workerRepository;
        private readonly IAttendanceRepository attendanceRepository = attendanceRepository;
        private readonly IWorkerRewardRepository workerRewardRepository = workerRewardRepository;

        [HttpGet("{workerId}")]
        public async Task<IActionResult> GetDashboard(Guid workerId)
        {
            var givingHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId,HabitType.Giving);
            var fastingHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId, HabitType.Fasting);
            var bibleStudyHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId,HabitType.BibleStudy);
            var nlpPrayerHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId, HabitType.NLPPrayer);
            var devotionalHabits = await this.habitRepository.GetHabitsByTypeAsync(workerId, HabitType.Devotionals);
            var worker = await this.workerRepository.GetUserByIdAsync(workerId);
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
                    Team = worker.Department.Team.Name
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
    }
}
