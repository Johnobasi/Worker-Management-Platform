using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Dtos.Habits;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IAttendanceRepository
    {
        Task AddAttendanceAsync(Attendance attendance);
        Task<bool> MarkAttendanceAsync(string qrCodeData);
        Task SaveAttendance(Guid workerId, DateTime checkInTime, AttendanceType attendanceType);
        Task<AttendanceSummaryResponse> GetWorkerAttendances(Guid workerId, DateTime startDate);
        Task<IEnumerable<AttendanceDto>> GetAllAttendancesAsync(DateTime? startDate, DateTime? endDate);
    }
}
