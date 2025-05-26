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
        }

        public async Task<bool> MarkAttendanceAsync(string qrCodeData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(qrCodeData))
                    throw new ArgumentException("QR code data is empty.");

                // Step 1: Extract WorkerNumber from the string
                string[] parts = qrCodeData.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 1)
                    throw new ArgumentException("Invalid QR code format. Expected: 'WORKERNUMBER FirstName LastName'");

                string workerNumber = parts[0];

                // Step 2: Look up worker by WorkerNumber
                var worker = await _userRepository.GetWorkerByNumberAsync(workerNumber);
                if (worker == null)
                    throw new KeyNotFoundException("User not found.");

                var qrCode = await _qrCodeRepository.GetBarcodeByWorkerIdAsync(worker.Id);
                if (qrCode == null || qrCode.IsActive)
                    throw new ArgumentException("QR code is invalid or disabled.");

                // Step 3: Mark the attendance
                var attendance = new Attendance
                {
                    WorkerId = worker.Id,
                    CheckInTime = DateTime.UtcNow,
                    Type = AttendanceType.SundayService,
                    CreatedAt = DateTime.UtcNow
                };

                await AddAttendanceAsync(attendance);
                _logger.LogInformation($"✅ Attendance marked for {worker.WorkerNumber} - {worker.FirstName} {worker.LastName}");
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
