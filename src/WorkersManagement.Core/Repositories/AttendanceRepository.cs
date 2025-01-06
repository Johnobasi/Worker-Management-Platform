using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Core.Repositories
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly WorkerDbContext _context;
        private readonly IQRCodeRepository _qrCodeRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AttendanceRepository> _logger;
        public AttendanceRepository(WorkerDbContext workerDbContext, 
            IQRCodeRepository qrCodeRepository, 
            IUserRepository userRepository, ILogger<AttendanceRepository> logger)
        {
            _context = workerDbContext;
            _qrCodeRepository = qrCodeRepository;
            _userRepository = userRepository;
            _logger = logger;
        }
        public async Task AddAttendanceAsync(Attendance attendance)
        {
            try
            {
                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        public async Task<IEnumerable<Attendance>> GetAllAttendancesAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Attendances
            .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate)
            .Include(a => a.Worker) // Include related Worker details if necessary
            .ToListAsync();
        }

        public async Task<IEnumerable<Attendance>> GetWorkerAttendances(Guid workerId, DateTime startDate)
        {
            _logger.LogInformation($"Getting worker attendances...{workerId.ToString()}");
            try
            {
                return await _context.Attendances
                    .Where(a => a.WorkerId == workerId && a.CheckInTime >= startDate)
                        .OrderByDescending(a => a.CheckInTime)
                            .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null!;
            }


           // return await _context.Attendances
           //.Include(a => a.Worker)
           //.Where(a => a.WorkerId == workerId &&
           //           a.CheckInTime >= startDate &&
           //           a.CheckInTime.DayOfWeek == DayOfWeek.Sunday)
           //.Select(a => new Attendance
           //{
           //    Id = a.Id,
           //    WorkerId = a.WorkerId,
           //    CheckInTime = a.CheckInTime,
           //    Status = a.Status,
           //    IsEarlyCheckIn = a.CheckInTime.Hour <= 8
           //})
           //.OrderByDescending(a => a.CheckInTime)
           //.ToListAsync();
        }

        public async Task<bool> MarkAttendanceAsync(string qrCodeData)
        {
            try
            {
                // Step 1: Decode and extract the user ID from the QR code
                var qrCodePayload = JsonSerializer.Deserialize<QRCode>(qrCodeData);
                if (qrCodePayload == null || qrCodePayload.UserId == Guid.Empty)
                    throw new ArgumentException("Invalid QR code data.");

                var userId = qrCodePayload.UserId;

                // Step 2: Validate the user and their QR code
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                    throw new KeyNotFoundException("User not found.");

                var qrCode = await _qrCodeRepository.GetQRCodeByIdAsync(userId);
                if (qrCode == null || qrCode.IsDisabled)
                    throw new ArgumentException("QR code is invalid or disabled.");

                // Step 3: Mark the attendance
                var attendance = new Attendance
                {
                    
                    Id = Guid.NewGuid(),
                    WorkerId = user.Id,
                    CheckInTime = DateTime.UtcNow,
                    Type = AttendanceType.SundayService,
                    CreatedAt = DateTime.UtcNow
                };

                await AddAttendanceAsync(attendance);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }

        public async Task SaveAttendance(Guid workerId, DateTime checkInTime)
        {
            _logger.LogInformation($"Saving attendance for worker {workerId.ToString()}...");
            try
            {
                var attendance = new Attendance
                {
                    Id = Guid.NewGuid(),
                    WorkerId = workerId,
                    CheckInTime = checkInTime,
                    Status = AttendanceStatus.Present
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }


    }
}
