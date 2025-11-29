using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Core.Repositories
{
    public class WorkerRewardRepository(
       IEmailService emailService,
        WorkerDbContext context, IWorkerManagementRepository user,
        ILogger<WorkerRewardRepository> logger) : IWorkerRewardRepository
    {
        private readonly IEmailService _emailService = emailService;
        private readonly WorkerDbContext _context = context;
        private readonly IWorkerManagementRepository _user = user;
        private readonly ILogger<WorkerRewardRepository> _logger = logger;

        public async Task CheckAndProcessReward(Guid workerId)
        {

            try
            {
                var now = DateTime.UtcNow;

                var monthStart = new DateTime(now.Year, now.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);


                // 1️⃣ Fetch all attendances in the week
                var monthlyAttendances = await _context.Attendances
                      .Where(a => a.WorkerId == workerId &&
                        a.CheckInTime >= monthStart &&
                        a.CheckInTime <= monthEnd)
                    .ToListAsync();

                // 2️⃣ Fetch all spiritual activities in the week
                var monthlyActivities = await _context.Habits
                     .Where(a => a.WorkerId == workerId &&
                        a.CompletedAt >= monthStart &&
                        a.CompletedAt <= monthEnd)
                    .ToListAsync();

                var sundayCutoff = new TimeSpan(9, 0, 0);       // 9:00 AM
                var midweekCutoff = new TimeSpan(18, 45, 0);    // 6:45 PM

                var sundayCount = monthlyAttendances.Count(a =>
                    a.Type == AttendanceType.SundayService &&
                    a.CheckInTime.DayOfWeek == DayOfWeek.Sunday &&
                    a.CheckInTime.TimeOfDay <= sundayCutoff);

                var midweekCount = monthlyAttendances.Count(a =>
                    a.Type == AttendanceType.MidweekService &&
                    a.CheckInTime.DayOfWeek == DayOfWeek.Wednesday &&
                    a.CheckInTime.TimeOfDay <= midweekCutoff);

                var specialEvents = monthlyAttendances.Count(a =>
                    a.Type == AttendanceType.SpecialServiceMeeting);

                var totalAttendance = sundayCount + midweekCount + specialEvents;

                var nlpCount = monthlyActivities.Count(a => a.Type == HabitType.NLPPrayer);
                var bibleCount = monthlyActivities.Count(a => a.Type == HabitType.BibleStudy);
                var devotionalCount = monthlyActivities.Count(a => a.Type == HabitType.Devotionals);
                var fastingCount = monthlyActivities.Count(a => a.Type == HabitType.Fasting);
                var givingCount = monthlyActivities.Count(a => a.Type == HabitType.Giving);

                var qualifiesForReward =
                     totalAttendance >= 4 &&
                     nlpCount >= 20 &&         // 5/week × 4 weeks (adjust as needed)
                     bibleCount >= 20 &&
                     devotionalCount >= 20 &&
                     fastingCount >= 8 &&      // 2/week × 4 weeks
                     givingCount >= 4;         // weekly giving (4 Sundays)

                if (!qualifiesForReward) return;

                var rewardEntity = await _context.Rewards
                        .FirstOrDefaultAsync(r => r.Type == RewardType.GiftVoucher);

                if (rewardEntity == null)
                {
                    // 2️⃣ Create the Reward if it does not exist
                    rewardEntity = new Reward
                    {
                        Id = Guid.NewGuid(),
                        Name = "Monthly Gift Voucher",
                        Description = "Awarded for outstanding spiritual commitment and participation.",
                        PointsRequired = 90,
                        Status = true,
                        Type = RewardType.GiftVoucher                                           
                    };

                    _context.Rewards.Add(rewardEntity);
                    await _context.SaveChangesAsync();
                }

                var reward = new WorkerReward
                {
                    Id = Guid.NewGuid(),
                    WorkerId = workerId,
                    RewardId = rewardEntity.Id,
                    RewardType = RewardType.GiftVoucher,
                    CreatedAt = DateTime.UtcNow,
                    Status = RewardStatus.Pending

                };

                await _context.WorkerRewards.AddAsync(reward);
                await _context.SaveChangesAsync();

                await NotifyWorker(workerId, monthlyAttendances, monthlyActivities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }           
        }

        public async Task ProcessAllRewardsAsync()
        {
            try
            {
                var allWorkers = await _context.Workers
                    .AsNoTracking()
                    .Select(w => w.Id)
                    .ToListAsync();

                foreach (var workerId in allWorkers)
                {
                    await CheckAndProcessReward(workerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing rewards for all workers.");
            }
        }

        public async Task NotifyWorker(Guid workerId, List<Attendance> monthlyAttendances, List<Habit> monthlyActivities)
        {
            try
            {
                var worker = await _user.GetWorkerByIdAsync(workerId);

                var emailSubject = "Congratulations! You've Earned a Gift Voucher";

                var emailBody = await LoadMonthlyTopWorkerRewardTemplateAsync(
                worker,
                monthlyAttendances,
                monthlyActivities);

                await _emailService.SendEmailAsync(worker.Email, emailSubject, emailBody);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            
        }

        private async Task<string> LoadMonthlyTopWorkerRewardTemplateAsync(
            Worker worker,
            IReadOnlyCollection<Attendance> weeklyAttendances,
            IReadOnlyCollection<Habit> weeklyActivities)
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string templatePath = Path.Combine(baseDirectory, "Templates", "MonthlyTopWorkerRewardTemplate.html");

                if (!File.Exists(templatePath))
                {
                    _logger.LogError("Reward email template not found at {Path}", templatePath);
                    throw new FileNotFoundException("Reward email template file not found", templatePath);
                }

                string template = await File.ReadAllTextAsync(templatePath);

                // Calculate counts once
                var sundayCount = weeklyAttendances.Count(a => a.Type == AttendanceType.SundayService);
                var midweekCount = weeklyAttendances.Count(a => a.Type == AttendanceType.MidweekService);
                var nlpPrayerCount = weeklyActivities.Count(a => a.Type == HabitType.NLPPrayer);
                var bibleStudyCount = weeklyActivities.Count(a => a.Type == HabitType.BibleStudy);
                var devotionalCount = weeklyActivities.Count(a => a.Type == HabitType.Devotionals);
                var fastingCount = weeklyActivities.Count(a => a.Type == HabitType.Fasting);
                var givingCount = weeklyActivities.Count(a => a.Type == HabitType.Giving);

                // Replace all placeholders
                template = template
                    .Replace("{FirstName}", worker.FirstName ?? "")
                    .Replace("{SundayCount}", sundayCount.ToString())
                    .Replace("{MidweekCount}", midweekCount.ToString())
                    .Replace("{NLPPrayerCount}", nlpPrayerCount.ToString())
                    .Replace("{BibleStudyCount}", bibleStudyCount.ToString())
                    .Replace("{DevotionalCount}", devotionalCount.ToString())
                    .Replace("{FastingCount}", fastingCount.ToString())
                    .Replace("{GivingCount}", givingCount.ToString());

                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load or populate MonthlyTopWorkerRewardTemplate for worker {WorkerId} - {Email}",
                    worker.Id, worker.Email);
                throw;
            }
        }



        public async Task SaveReward(WorkerReward reward)
        {
            try
            {
                _context.WorkerRewards.Add(reward);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        public async Task ResetConsecutiveCount(Guid workerId)
        {

            try
            {
                var worker = await _context.Workers
                    .FirstOrDefaultAsync(w => w.Id == workerId);
                if (worker != null)
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }
        public async Task<WorkerRewardResponse> GetRewardsForWorkerAsync(Guid workerId)
        {

            try
            {
                var rewards = await _context.WorkerRewards
                    .Where(r => r.WorkerId == workerId)
                        .OrderByDescending(r => r.CreatedAt)
                             .ToListAsync();

                var response = new WorkerRewardResponse
                {
                    Message = rewards.Count == 0
                        ? "The worker has no rewards at this time."
                        : $"🏆🏆🏆 Congratulations! 🎉🎈💐\n\n" +
                        $"You've earned a Gift Voucher this month.\nPlease see your team pastor to receive it."
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new WorkerRewardResponse
                {
                    Message = "An error occurred while retrieving rewards."
                };
            }

        }
    }

    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.Date.AddDays(-1 * diff);
        }
    }
}
