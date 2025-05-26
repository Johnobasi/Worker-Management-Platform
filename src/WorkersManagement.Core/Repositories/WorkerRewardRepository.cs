using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Core.Repositories
{
    public class WorkerRewardRepository(
        IAttendanceRepository attendanceRepository, IEmailService emailService,
        WorkerDbContext context, IWorkerManagementRepository user, IConfiguration configuration,
        ILogger<WorkerRewardRepository> logger) : IWorkerRewardRepository
    {
        private readonly IAttendanceRepository _attendanceRepository = attendanceRepository;
        private readonly IEmailService _emailService = emailService;
        private readonly IConfiguration configuratin = configuration;
        private readonly int _consecutiveSundaysRequired = configuration!.GetValue<int>("RewardSettings:ConsecutiveSundaysRequired");
        private readonly int _earlyCheckInHour = configuration!.GetValue<int>("RewardSettings:EarlyCheckInHour");
        private readonly WorkerDbContext _context = context;
        private readonly IWorkerManagementRepository _user = user;
        private readonly ILogger<WorkerRewardRepository> _logger = logger;

        public async Task ProcessSundayAttendance(Guid workerId, DateTime checkInTime)
        {
            _logger.LogInformation($"Processing Sunday attendance for worker {workerId}");
            try
            {
                if (!IsSunday(checkInTime)) return;

                var isEarlyCheckIn = IsEarlyCheckIn(checkInTime);
                if (!isEarlyCheckIn) return;

                await _attendanceRepository.SaveAttendance(workerId, checkInTime);
                await CheckAndProcessReward(workerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

            }

        }
        public async Task CheckAndProcessReward(Guid workerId)
        {
            var consecutiveSundays = await GetConsecutiveEarlySundays(workerId);

            if (consecutiveSundays >= _consecutiveSundaysRequired)
            {
                await CreateReward(workerId);
                await NotifyWorker(workerId);
                await ResetConsecutiveCount(workerId);
            }
        }
        public async Task<int> GetConsecutiveEarlySundays(Guid workerId)
        {
            var attendances = await _attendanceRepository.GetWorkerAttendances(
                workerId,
                DateTime.UtcNow.AddDays(-_consecutiveSundaysRequired * 7)
            );

            return CalculateConsecutiveSundays(attendances);
        }

        public int CalculateConsecutiveSundays(IEnumerable<Attendance> attendances)
        {
            var count = 0;
            var lastSunday = DateTime.UtcNow.Date;

            foreach (var attendance in attendances.OrderByDescending(a => a.CheckInTime))
            {
                if (!IsSunday(attendance.CheckInTime) ||
                    !IsEarlyCheckIn(attendance.CheckInTime) ||
                    !IsConsecutiveSunday(attendance.CheckInTime, lastSunday))
                {
                    break;
                }

                count++;
                lastSunday = attendance.CheckInTime.Date.AddDays(-7);
            }

            return count;
        }

        public async Task CreateReward(Guid workerId)
        {
            try
            {
                var reward = new WorkerReward
                {
                    Id = Guid.NewGuid(),
                    WorkerId = workerId,
                    RewardType = RewardType.GiftVoucher,
                    CreatedAt = DateTime.UtcNow,
                    Status = RewardStatus.Pending
                };

                await _context.WorkerRewards.AddAsync(reward);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        public async Task NotifyWorker(Guid workerId)
        {
            var user = await _user.GetWorkerByIdAsync(workerId);

            var emailSubject = "Congratulations! You've Earned a Gift Voucher";
            var emailBody = $@"
            Dear {user.FirstName},
            
            Congratulations! You have successfully maintained early attendance for 15 consecutive Sundays.
            As a token of our appreciation, you have earned a gift voucher.
            
            Please collect your gift voucher from the church office.
            
            Thank you for your dedication and commitment.
            
            Best regards,
            Church Management Team
            ";

            await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);
        }

        public bool IsSunday(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Sunday;
        }

        public bool IsEarlyCheckIn(DateTime checkInTime)
        {
            var configuredHour = _earlyCheckInHour;
            return checkInTime.Hour <= configuredHour;
        }
        public static bool IsConsecutiveSunday(DateTime current, DateTime previous)
        {
            return (previous - current).Days == 7;
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
                     .ToListAsync();
                var response = new WorkerRewardResponse
                {
                    Rewards = rewards,
                    Message = rewards.Count == 0 ? "The worker has no rewards at this time." : "Rewards retrieved successfully."
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
}
