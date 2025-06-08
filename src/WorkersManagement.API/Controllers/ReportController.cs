using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IHabitRepository _habitRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        public ReportController(IHabitRepository habitRepository, IAttendanceRepository attendanceRepository)
        {
            _attendanceRepository = attendanceRepository;
            _habitRepository = habitRepository;
        }

        [HttpGet("worker-activity-summary")]
        [Authorize(Policy = "HOD")]
        public async Task<IActionResult> GetWorkerActivitySummary(
              [FromQuery] bool isAdmin,
              [FromQuery] DateTime? startDate = null,
              [FromQuery] DateTime? endDate = null,
              [FromQuery] string? teamName = null,
              [FromQuery] string? departmentName = null)
        {
            if (!isAdmin)
                return Forbid("Only admins can view this report.");

            startDate ??= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            endDate ??= DateTime.UtcNow;

            var attendanceData = await _attendanceRepository.GetAllAttendancesAsync(startDate.Value, endDate.Value);

            if (!string.IsNullOrWhiteSpace(departmentName))
            {
                departmentName = departmentName.Trim().ToLower();
                attendanceData = attendanceData
                    .Where(a => a.Worker.Department != null &&
                                a.Worker.Department.Name.ToLower() == departmentName)
                    .ToList();
            }
            else if (!string.IsNullOrWhiteSpace(teamName))
            {
                teamName = teamName.Trim().ToLower();
                attendanceData = attendanceData
                    .Where(a => a.Worker.Department?.Teams != null &&
                                a.Worker.Department.Teams.Name.ToLower() == teamName)
                    .ToList();
            }

            var workerIds = attendanceData.Select(a => a.WorkerId).Distinct().ToList();

            var habitsData = await _habitRepository.GetHabitsByWorkerIdAsync(workerIds);
            habitsData = habitsData
                .Where(h => h.CompletedAt >= startDate.Value && h.CompletedAt <= endDate.Value)
                .ToList();

            var summaries = attendanceData
                .GroupBy(a => a.Worker)
                .Select(g =>
                {
                    var worker = g.Key;
                    var habitCount = habitsData.Count(h => h.WorkerId == worker.Id);

                    return new
                    {
                        WorkerId = worker.Id,
                        Name = $"{worker.FirstName} {worker.LastName}",
                        Email = worker.Email,
                        Department = worker.Department?.Name,
                        Team = worker.Department?.Teams?.Name,
                        AttendanceCount = g.Count(),
                        HabitCount = habitCount
                    };
                })
                .OrderBy(s => s.Name)
                .ToList();

            var csv = GenerateCsv(summaries);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"worker_activity_summary_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        [HttpGet("export-attendance")]
        [Authorize(Policy = "HOD")]
        public async Task<IActionResult> ExportAttendanceReport(
         [FromQuery] bool isAdmin,
         [FromQuery] DateTime? startDate = null,
         [FromQuery] DateTime? endDate = null,
         [FromQuery] string? teamName = null,
         [FromQuery] string? departmentName = null)
        {
            if (!isAdmin)
                return Forbid("Only admins can export reports.");

            startDate ??= DateTime.UtcNow.AddMonths(-1);
            endDate ??= DateTime.UtcNow;

            var attendanceData = await _attendanceRepository.GetAllAttendancesAsync(startDate.Value, endDate.Value);

            // Apply department/team name filters
            if (!string.IsNullOrWhiteSpace(departmentName))
            {
                departmentName = departmentName.Trim().ToLower();
                attendanceData = attendanceData
                    .Where(a => a.Worker.Department != null &&
                                a.Worker.Department.Name.ToLower() == departmentName)
                    .ToList();
            }
            else if (!string.IsNullOrWhiteSpace(teamName))
            {
                teamName = teamName.Trim().ToLower();
                attendanceData = attendanceData
                    .Where(a => a.Worker.Department?.Teams != null &&
                                a.Worker.Department.Teams.Name.ToLower() == teamName)
                    .ToList();
            }

            var workerIds = attendanceData.Select(a => a.WorkerId).Distinct().ToList();

            var habitsData = await _habitRepository.GetHabitsByWorkerIdAsync(workerIds);
            habitsData = habitsData
                .Where(h => h.CompletedAt >= startDate.Value && h.CompletedAt <= endDate.Value)
                .ToList();

            var attendanceCsv = GenerateCsv(attendanceData.Select(a => new
            {
                WorkerId = a.WorkerId,
                WorkerName = $"{a.Worker.FirstName} {a.Worker.LastName}",
                Date = a.CreatedAt.ToString("yyyy-MM-dd"),
                Status = a.Status,
                Department = a.Worker.Department?.Name,
                Team = a.Worker.Department?.Teams?.Name
            }));

            var habitsCsv = GenerateCsv(habitsData.Select(h => new
            {
                WorkerId = h.WorkerId,
                WorkerName = $"{h.Worker.FirstName} {h.Worker.LastName}",
                HabitType = h.Type.ToString(),
                CompletedAt = h.CompletedAt.ToString("yyyy-MM-dd"),
                Notes = h.Notes,
                Amount = h.Type == HabitType.Giving ? h.Amount : null,
                Department = h.Worker.Department?.Name,
                Team = h.Worker.Department?.Teams?.Name
            }));

            var combined = $"Attendance Report\n{attendanceCsv}\n\nHabit Report\n{habitsCsv}";
            return File(Encoding.UTF8.GetBytes(combined), "text/csv", $"attendance_report_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        [HttpGet("export-summary")]
        [Authorize(Policy = "HOD")]
        public async Task<IActionResult> ExportWorkerSummaryReport(
          [FromQuery] bool isAdmin,
          [FromQuery] DateTime? startDate = null,
          [FromQuery] DateTime? endDate = null)
        {
            if (!isAdmin)
                return Forbid("Only admins can export summary.");

            startDate ??= DateTime.UtcNow.AddMonths(-1);
            endDate ??= DateTime.UtcNow;

            var attendanceData = await _attendanceRepository.GetAllAttendancesAsync(startDate.Value, endDate.Value);

            var departmentSummary = attendanceData
                .GroupBy(a => a.Worker.Department.Name)
                .Select(g => new
                {
                    Department = g.Key,
                    WorkerCount = g.Select(a => a.WorkerId).Distinct().Count()
                });

            var teamSummary = attendanceData
                .GroupBy(a => a.Worker.Department.Teams.Name)
                .Select(g => new
                {
                    Team = g.Key,
                    WorkerCount = g.Select(a => a.WorkerId).Distinct().Count()
                });

            var deptCsv = GenerateCsv(departmentSummary);
            var teamCsv = GenerateCsv(teamSummary);

            var combined = $"Department Summary\n{deptCsv}\n\nTeam Summary\n{teamCsv}";
            return File(Encoding.UTF8.GetBytes(combined), "text/csv", $"worker_summary_{DateTime.UtcNow:yyyyMMdd}.csv");
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
