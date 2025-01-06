using WorkersManagement.Infrastructure;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IAttendanceRepository
    {
        Task AddAttendanceAsync(Attendance attendance);
        Task<bool> MarkAttendanceAsync(string qrCodeData);
        Task SaveAttendance(Guid workerId, DateTime checkInTime);
        Task<IEnumerable<Attendance>> GetWorkerAttendances(Guid workerId, DateTime startDate);
        Task<IEnumerable<Attendance>> GetAllAttendancesAsync(DateTime startDate, DateTime endDate);
    }
}
