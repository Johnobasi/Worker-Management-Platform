using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Dtos.Habits;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Core.Repositories
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly WorkerDbContext _context;
        private readonly IBarcodeRepository _qrCodeRepository;
        private readonly IWorkerManagementRepository _userRepository;
        private readonly ILogger<AttendanceRepository> _logger;
        public AttendanceRepository(WorkerDbContext workerDbContext, 
            IBarcodeRepository qrCodeRepository, 
            IWorkerManagementRepository userRepository, ILogger<AttendanceRepository> logger)
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

        public async Task<IEnumerable<AttendanceDto>> GetAllAttendancesAsync(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Attendances.AsQueryable();

            // Apply date filter only if provided
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= startDate.Value && a.CreatedAt <= endDate.Value);
            }

            // Select into DTO to include only necessary Worker info
            return await query
                .Select(a => new AttendanceDto
                {
                    Id = a.Id,
                    WorkerId = a.WorkerId,
                    CheckInTime = a.CheckInTime,
                    Type = a.Type.ToString(),
                    CreatedAt = a.CreatedAt,
                    Status = a.Status.ToString(),
                    IsEarlyCheckIn = a.IsEarlyCheckIn,
                    Worker = new WorkerDto
                    {
                        Id = a.Worker.Id,
                        WorkerNumber = a.Worker.WorkerNumber,
                        FirstName = a.Worker.FirstName,
                        LastName = a.Worker.LastName,
                        Email = a.Worker.Email,
                        DepartmentName = a.Worker.Department.Name,
                        TeamName = a.Worker.Department.Teams.Name
                    }
                })
                .ToListAsync();
        }

        public async Task<AttendanceSummaryResponse> GetWorkerAttendances(Guid workerId, DateTime startDate)
        {
            _logger.LogInformation($"Getting worker attendances...{workerId.ToString()}");
            try
            {

                var attendances = await _context.Attendances
                     .Where(a => a.WorkerId == workerId)
                     .ToListAsync();

                var worker = await _context.Workers
                        .Where(w => w.Id == workerId)
                            .FirstOrDefaultAsync();

                // Count each attendance type
                var sundayServiceCount = attendances.Count(a => a.Type == AttendanceType.SundayService);
                var midweekServiceCount = attendances.Count(a => a.Type == AttendanceType.MidweekService);
                var workersMeetingCount = attendances.Count(a => a.Type == AttendanceType.WorkersMeeting);
                var specialCount = attendances.Count(a => a.Type == AttendanceType.SpecialServiceMeeting);
                var totalCount = sundayServiceCount + midweekServiceCount + specialCount + workersMeetingCount;

                // Build summary message
                var summary = $"Hi {worker!.FirstName}, here’s your attendance summary this month:\n" +
                              $"- Sunday Services: {sundayServiceCount}\n" +
                              $"- Midweek Services: {midweekServiceCount}\n" +
                              $"- Workers Meetings: {workersMeetingCount}\n" +
                              $"- Special Meetings: {specialCount}\n " +
                              $"- Total Attendances as at now: {totalCount}\n\n" +
                              $"Keep up the great participation! 🌟";

                return new AttendanceSummaryResponse
                {
                    SummaryMessages =  summary 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null!;
            }
        }

        public async Task<bool> MarkAttendanceAsync(string qrCodeData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(qrCodeData))
                    throw new ArgumentException("QR code data is empty.");

                // split scanned barcode content -> must contain workerNumber first
                string[] parts = qrCodeData.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    throw new ArgumentException("Invalid QR code format. Expected: 'WORKERNUMBER FirstName LastName'");

                string workerNumber = parts[0];

                // find worker by worker number
                var worker = await _userRepository.GetWorkerByNumberAsync(workerNumber);
                if (worker == null)
                    throw new KeyNotFoundException("Worker not found.");

                // find their active barcode
                var qrCode = await _qrCodeRepository.GetBarcodeByWorkerIdAsync(worker.Id);
                if (qrCode == null || !qrCode.IsActive)
                    throw new ArgumentException("QR code is invalid or disabled.");

                // create attendance record
                var attendance = new Attendance
                {
                    WorkerId = worker.Id,
                    CheckInTime = DateTime.UtcNow,
                    Type = AttendanceType.SundayService,
                    CreatedAt = DateTime.UtcNow
                };

                await AddAttendanceAsync(attendance);

                _logger.LogInformation($"Attendance marked for {worker.WorkerNumber} - {worker.FirstName} {worker.LastName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }

        public async Task SaveAttendance(Guid workerId, DateTime checkInTime, AttendanceType attendanceType)
        {
            _logger.LogInformation($"Saving attendance for worker {workerId.ToString()}...");
            if (!IsValidAttendanceTime(attendanceType, checkInTime))
            {
                string message = attendanceType switch
                {
                    AttendanceType.SundayService =>
                        "You can only mark Sunday Service attendance on Sundays between 8:30 AM and 7:00 PM.",

                    AttendanceType.MidweekService =>
                        "You can only mark Midweek Service attendance on Wednesdays between 6:45 PM and 8:30 PM.",

                    _ => "Attendance cannot be marked at this time."
                };

                throw new InvalidOperationException(message);
            }
            try
            {

                // Determine IsEarlyCheckIn based on your cutoff times
                var sundayCutoff = new TimeSpan(9, 0, 0);         // 9:00 AM
                var midweekCutoff = new TimeSpan(18, 45, 0);      // 6:45 PM

                bool isEarly = attendanceType switch
                {
                    AttendanceType.SundayService =>
                        checkInTime.DayOfWeek == DayOfWeek.Sunday &&
                        checkInTime.TimeOfDay <= sundayCutoff,

                    AttendanceType.MidweekService =>
                        checkInTime.DayOfWeek == DayOfWeek.Wednesday &&
                        checkInTime.TimeOfDay <= midweekCutoff,

                    AttendanceType.SpecialServiceMeeting => true, // always early for special

                    _ => false
                };

                var attendance = new Attendance
                {
                    Id = Guid.NewGuid(),
                    WorkerId = workerId,
                    CheckInTime = checkInTime,
                    Status = AttendanceStatus.Present,
                    Type = attendanceType,
                    CreatedAt = DateTime.UtcNow,
                    IsEarlyCheckIn = isEarly
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private bool IsValidAttendanceTime(AttendanceType type, DateTime checkInTime)
        {
            var timeOfDay = checkInTime.TimeOfDay;
            var dayOfWeek = checkInTime.DayOfWeek;

            return type switch
            {
                AttendanceType.SundayService =>
                    dayOfWeek == DayOfWeek.Sunday &&
                    timeOfDay >= new TimeSpan(8, 0, 0) && timeOfDay <= new TimeSpan(19, 0, 0),

                AttendanceType.MidweekService =>
                    dayOfWeek == DayOfWeek.Wednesday &&
                    timeOfDay >= new TimeSpan(18, 45, 0) && timeOfDay <= new TimeSpan(20, 30, 0),

                _ => true // other attendance types are not restricted
            };
        }
        private string BuildAttendanceMessage(string name, AttendanceType type, int monthlyCount)
        {
            return type switch
            {
                AttendanceType.SundayService =>
                    $"Hi {name}, you have attended {monthlyCount} Sunday services this month.",

                AttendanceType.MidweekService =>
                    $"Hi {name}, you have attended {monthlyCount} Wednesday midweek services this month.",

                AttendanceType.SpecialServiceMeeting =>
                    $"Hi {name}, you have attended {monthlyCount} special meetings this month.",

                _ => $"Hi {name}, thank you for staying consistent in attendance!"
            };
        }

    }
}
