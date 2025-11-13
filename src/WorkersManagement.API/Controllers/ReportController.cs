using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using WorkersManagement.Domain.Interfaces;

namespace WorkersManagement.API.Controllers
{
    /// <summary>
    /// Generate and export worker activity reports
    /// </summary>
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

        /// <summary>
        /// Get worker activity summary as CSV
        /// </summary>
        /// <param name="startDate">Report start date</param>
        /// <param name="endDate">Report end date</param>
        /// <param name="teamName">Filter by team name</param>
        /// <param name="departmentName">Filter by department name</param>
        /// <returns>CSV file with worker activity data</returns>
        [HttpGet("worker-activity-summary")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWorkerActivitySummary(
              [FromQuery] DateTime? startDate ,
              [FromQuery] DateTime? endDate ,
              [FromQuery] string? teamName = null,
              [FromQuery] string? departmentName = null)
        {


            var attendanceData = await _attendanceRepository.GetAllAttendancesAsync(startDate, endDate);

            if (!string.IsNullOrWhiteSpace(departmentName))
            {
                departmentName = departmentName.Trim().ToLower();
                attendanceData = attendanceData
                    .Where(a => 
                                a.Worker.DepartmentName == departmentName)
                    .ToList();
            }
            else if (!string.IsNullOrWhiteSpace(teamName))
            {
                teamName = teamName.Trim().ToLower();
                attendanceData = attendanceData
                    .Where(a =>
                             a.Worker.TeamName.ToLower() == teamName)
                    .ToList();
            }

            var workerIds = attendanceData.Select(a => a.WorkerId).Distinct().ToList();

            var habitsData = await _habitRepository.GetHabitsByWorkerIdAsync(workerIds);
            habitsData = habitsData
                .Where(h => h.CompletedAt >= startDate && h.CompletedAt <= endDate)
                .ToList();

            var summaries = attendanceData
                .GroupBy(a => a.Worker)
                .Select(g =>
                {
                    var worker = g.Key;
                    var habitCount = habitsData.Count(h => h.WorkerId == worker.Id);

                    return new
                    {
                        WorkerNumber = worker.WorkerNumber,
                        Name = $"{worker.FirstName} {worker.LastName}",
                        Email = worker.Email,
                        Department = worker.DepartmentName,
                        Team = worker.TeamName,
                        AttendanceCount = g.Count(),
                        HabitCount = habitCount
                    };
                })
                .OrderBy(s => s.Name)
                .ToList();

            var csv = GenerateCsv(summaries);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"worker_activity_summary_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        /// <summary>
        /// Export detailed attendance and habit report
        /// </summary>
        /// <param name="startDate">Report start date</param>
        /// <param name="endDate">Report end date</param>
        /// <param name="teamName">Filter by team name</param>
        /// <param name="departmentName">Filter by department name</param>
        /// <returns>CSV file with detailed attendance and habit data</returns>
        [HttpGet("export-attendance")]
        [AllowAnonymous]
        public async Task<IActionResult> ExportAttendanceReport(
         [FromQuery] DateTime? startDate,
         [FromQuery] DateTime? endDate ,
         [FromQuery] string? teamName,
         [FromQuery] string? departmentName)
        {

            var attendanceData = await _attendanceRepository.GetAllAttendancesAsync(startDate, endDate);

            // Apply department/team name filters
            if (!string.IsNullOrWhiteSpace(departmentName))
            {
                departmentName = departmentName.Trim().ToLower();
                attendanceData = attendanceData
                    .Where(a => 
                                a.Worker.DepartmentName.ToLower() == departmentName)
                    .ToList();
            }
            else if (!string.IsNullOrWhiteSpace(teamName))
            {
                teamName = teamName.Trim().ToLower();
                attendanceData = attendanceData
                    .Where(a => 
                                a.Worker.DepartmentName.ToLower() == teamName)
                    .ToList();
            }

            var workerIds = attendanceData.Select(a => a.WorkerId).Distinct().ToList();

            var habitsData = await _habitRepository.GetHabitsByWorkerIdAsync(workerIds);
            habitsData = habitsData
                .Where(h => h.CompletedAt >= startDate && h.CompletedAt <= endDate)
                .ToList();

            var attendanceCsv = GenerateCsv(attendanceData.Select(a => new
            {
                WorkerNumber = a.Worker.WorkerNumber,
                WorkerName = $"{a.Worker.FirstName} {a.Worker.LastName}",
                Date = a.CreatedAt.ToString("yyyy-MM-dd"),
                Status = a.Status,
                Department = a.Worker.DepartmentName,
                Team = a.Worker.TeamName
            }));

            var combined = $"Attendance Report\n{attendanceCsv}";
            return File(Encoding.UTF8.GetBytes(combined), "text/csv", $"attendance_report_{DateTime.UtcNow:yyyyMMdd}.csv");
        }


        /// <summary>
        /// Export department and team summary report
        /// </summary>
        /// <param name="startDate">Report start date</param>
        /// <param name="endDate">Report end date</param>
        /// <returns>CSV file with department and team summaries</returns>
        [HttpGet("export-summary")]
        [AllowAnonymous]
        public async Task<IActionResult> ExportWorkerSummaryReport(
          [FromQuery] DateTime? startDate,
          [FromQuery] DateTime? endDate)
        {


            var attendanceData = await _attendanceRepository.GetAllAttendancesAsync(startDate, endDate);

            var departmentSummary = attendanceData
                .GroupBy(a => a.Worker.DepartmentName)
                .Select(g => new
                {
                    Department = g.Key,
                    WorkerCount = g.Select(a => a.WorkerId).Distinct().Count()
                });

            var teamSummary = attendanceData
                .GroupBy(a => a.Worker.TeamName)
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
