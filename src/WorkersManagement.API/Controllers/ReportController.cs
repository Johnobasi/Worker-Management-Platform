using Microsoft.AspNetCore.Mvc;
using System.Text;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IHabitRepository _habitRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        public ReportController(IHabitRepository habitRepository, IAttendanceRepository attendanceRepository)
        {
            _attendanceRepository = attendanceRepository;
            _habitRepository = habitRepository;


        }


        [HttpGet("export-report")]
        public async Task<IActionResult> ExportReport(
             [FromQuery] bool isAdmin,
             [FromQuery] DateTime? startDate = null,
             [FromQuery] DateTime? endDate = null,
             [FromQuery] Guid? teamId = null,
               [FromQuery] Guid? departmentId = null)
        {
            if (!isAdmin)
                return Forbid("Only admins can export reports.");

            // Default date range if not provided
            startDate ??= DateTime.UtcNow.AddMonths(-1);
            endDate ??= DateTime.UtcNow;

            // Fetch attendance and habit data
            var attendanceData = await _attendanceRepository.GetAllAttendancesAsync(startDate.Value, endDate.Value);

            // Filter attendance by department or team
            if (departmentId.HasValue)
            {
                attendanceData = attendanceData.Where(a => a.Worker.DepartmentId == departmentId).ToList();
            }
            else if (teamId.HasValue)
            {
                attendanceData = attendanceData.Where(a => a.Worker.Department.TeamId == teamId).ToList();
            }

            var workerIds = attendanceData.Select(a => a.WorkerId).Distinct().ToList();

            // Fetch habit data for filtered workers
            var habitsData = await _habitRepository.GetHabitsByWorkerIdAsync(workerIds);

            // Generate CSV content
            var attendanceCsv = GenerateCsv(attendanceData.Select(a => new
            {
                WorkerId = a.WorkerId,
                WorkerName = a.Worker.FirstName + " " + a.Worker.LastName,
                Date = a.CreatedAt.ToString("yyyy-MM-dd"),
                Status = a.Status,
                Department = a.Worker.Department.Name,
                Team = a.Worker.Department.Teams.Name
            }));

            var habitsCsv = GenerateCsv(habitsData.Select(h => new
            {
                WorkerId = h.WorkerId,
                WorkerName = h.Worker.FirstName + " " + h.Worker.LastName,
                HabitType = h.Type.ToString(),
                CompletedAt = h.CompletedAt.ToString("yyyy-MM-dd"),
                Notes = h.Notes,
                Amount = h.Type == HabitType.Giving ? h.Amount : null,
                Department = h.Worker.Department.Name,
                Team = h.Worker.Department.Teams.Name
            }));

            var combinedCsv = $"Attendance Report\n{attendanceCsv}\n\nHabit Report\n{habitsCsv}";

            var bytes = Encoding.UTF8.GetBytes(combinedCsv);

            // Return as a downloadable CSV file
            return File(bytes, "text/csv", "report.csv");
        }

        #region reports helpers
        private string GenerateCsv<T>(IEnumerable<T> data)
        {
            if (!data.Any()) return string.Empty;

            var properties = typeof(T).GetProperties();
            var csv = new StringBuilder();

            // Add header row
            csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));

            // Add data rows
            foreach (var item in data)
            {
                csv.AppendLine(string.Join(",", properties.Select(p => $"\"{p.GetValue(item)?.ToString()?.Replace("\"", "\"\"")}\"")));
            }

            return csv.ToString();
        }

        #endregion
    }
}
