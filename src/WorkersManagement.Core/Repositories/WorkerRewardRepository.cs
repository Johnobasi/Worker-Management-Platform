using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
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

                var reward = new WorkerReward
                {
                    Id = Guid.NewGuid(),
                    WorkerId = workerId,
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

        public async Task NotifyWorker(Guid workerId, List<Attendance> weeklyAttendances, List<Habit> weeklyActivities)
        {
            try
            {
                var worker = await _user.GetWorkerByIdAsync(workerId);

                var emailSubject = "Congratulations! You've Earned a Gift Voucher";
                var emailBody = $@"
                Dear {worker.FirstName},
            
                Congratulations! 🌟

                This month, you have recorded the highest spiritual habits among your peers. 
                
                Here’s a summary of your activities:

                - Early Sunday Attendance: {weeklyAttendances.Count(a => a.Type == AttendanceType.SundayService)} days
                - Midweek Service Attendance: {weeklyAttendances.Count(a => a.Type == AttendanceType.MidweekService)} days
                - NLP Prayer: {weeklyActivities.Count(a => a.Type == HabitType.NLPPrayer)} times
                - Bible Reading: {weeklyActivities.Count(a => a.Type == HabitType.BibleStudy)} times
                - Devotional: {weeklyActivities.Count(a => a.Type == HabitType.Devotionals)} days
                - Fasting: {weeklyActivities.Count(a => a.Type == HabitType.Fasting)} days
                - Giving: {weeklyActivities.Count(a => a.Type == HabitType.Giving)} Sundays

                As a token of our appreciation for your dedication and faithfulness, you have earned a gift voucher. 

                Please collect your gift voucher from the church office at your convenience.

                Thank you for your commitment and inspiring example!
            
                Best regards,
                Church Management Team
                ";

                await _emailService.SendEmailAsync(worker.Email, emailSubject, emailBody);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
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
                    Rewards = rewards,
                    Message = rewards.Count == 0 ? "The worker has no rewards at this time." : "🎉 Congratulations!\n\n" +                         
                                                    "You’ve earned a Gift Voucher for your outstanding participation and spiritual commitment this month.\n" +
                                                    "Thank you for your consistency in services and spiritual habits! \n" +
                                                    "Please your team pastor for your Gift Voucher!"
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
