using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using WorkersManagement.Domain.Dtos.Habits;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;

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
        private readonly ILogger<ReportController> _logger;
        public ReportController(IHabitRepository habitRepository, IAttendanceRepository attendanceRepository, ILogger<ReportController> logger)
        {
            _attendanceRepository = attendanceRepository;
            _habitRepository = habitRepository;
            _logger = logger;
        }

        /// <summary>
        /// Download worker activity summary as CSV
        /// </summary>
        [HttpPost("download-worker-activity-summary")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadWorkerActivitySummary(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? teamName = null,
            [FromQuery] string? departmentName = null,
            [FromQuery] List<Guid>? selectedWorkerIds = null)
        {

            try
            {
                var attendanceData = await _attendanceRepository.GetAllAttendancesAsync(
                startDate, endDate);

                attendanceData = ApplyFilters(attendanceData, teamName, departmentName, selectedWorkerIds);

                var workerIds = attendanceData.Select(a => a.WorkerId).Distinct().ToList();
                var habitsData = await _habitRepository.GetHabitsByWorkerIdAsync(workerIds);

                if (startDate.HasValue && endDate.HasValue)
                {
                    habitsData = habitsData
                        .Where(h => h.CompletedAt >= startDate && h.CompletedAt <= endDate)
                        .ToList();
                }

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
                            PresentCount = g.Count(a => a.Status == "Present"),
                            AbsentCount = g.Count(a => a.Status == "Absent"),
                            LateCount = g.Count(a => a.Status == "Late"),
                            HabitCount = habitCount,
                            AttendanceRate = g.Any() ? ((double)g.Count(a => a.Status == "Present") / g.Count() * 100).ToString("F2") + "%" : "0%"
                        };
                    })
                    .OrderBy(s => s.Name)
                    .ToList();

                var csv = GenerateCsv(summaries, "WORKER ACTIVITY SUMMARY REPORT");
                var filename = $"worker_activity_summary_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                return File(Encoding.UTF8.GetBytes(csv), "text/csv", filename);
            }
            catch (Exception ex)
            {
               _logger.LogError(ex, "Error generating worker activity summary report");
                return BadRequest(new { Message = "An error occurred while generating the report." });
            }
            
        }

        /// <summary>
        /// Download detailed attendance and habit report
        /// </summary>
        [HttpPost("download-attendance-report")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadAttendanceReport(
             [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? teamName = null,
            [FromQuery] string? departmentName = null,
            [FromQuery] List<Guid>? selectedWorkerIds = null)
        {
            try
            {
                var attendanceData = await _attendanceRepository.GetAllAttendancesAsync(
                startDate, endDate);

                attendanceData = ApplyFilters(attendanceData, teamName, departmentName, selectedWorkerIds);

                var workerIds = attendanceData.Select(a => a.WorkerId).Distinct().ToList();
                var habitsData = await _habitRepository.GetHabitsByWorkerIdAsync(workerIds);

                if (startDate.HasValue && endDate.HasValue)
                {
                    habitsData = habitsData
                        .Where(h => h.CompletedAt >= startDate && h.CompletedAt <= endDate)
                        .ToList();
                }

                var detailedData = new List<AttendanceHabitRecordDto>();
                    foreach(var attendance in attendanceData)
                    {

                        // Attendance record
                        detailedData.Add(new AttendanceHabitRecordDto
                        {
                            WorkerNumber = attendance.Worker.WorkerNumber,
                            WorkerName = $"{attendance.Worker.FirstName} {attendance.Worker.LastName}",
                            Date = attendance.CreatedAt.ToString("yyyy-MM-dd"),
                            Type = "Attendance",
                            Status = attendance.Status,
                            Department = attendance.Worker.DepartmentName,
                            Team = attendance.Worker.TeamName,
                            Details = attendance.Status,
                            HabitName = "",
                            HabitCompletedAt = ""
                        });

                        // Add habit records for the same day
                        var workerHabits = habitsData
                            .Where(h => h.WorkerId == attendance.WorkerId &&
                                       h.CompletedAt.Date == attendance.CreatedAt.Date)
                            .ToList();
                        // Habit records for the same day
                        foreach (var habit in workerHabits)
                        {
                            detailedData.Add(new AttendanceHabitRecordDto
                            {
                                WorkerNumber = attendance.Worker.WorkerNumber,
                                WorkerName = $"{attendance.Worker.FirstName} {attendance.Worker.LastName}",
                                Date = habit.CompletedAt.ToString("yyyy-MM-dd"),
                                Type = "Spiritual Habit",
                                Status = "Completed",
                                Department = attendance.Worker.DepartmentName,
                                Team = attendance.Worker.TeamName,
                                Details = habit.Notes ?? string.Empty,
                                HabitName = habit.Type.ToString(),
                                HabitCompletedAt = habit.CompletedAt.ToString("yyyy-MM-dd HH:mm")
                            });
                        }
                    }

                var csv = GenerateCsv(detailedData, "DETAILED ATTENDANCE REPORT");
                var filename = $"detailed_attendance_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                return File(Encoding.UTF8.GetBytes(csv), "text/csv", filename);
            }
            catch (Exception ex)
            {
               _logger.LogError(ex, "Error generating detailed attendance report");
                return BadRequest(new { Message = "An error occurred while generating the report." });
            }           
        }


        /// <summary>
        /// Download department and team summary report
        /// </summary>
        [HttpPost("download-summary-report")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadSummaryReport(
             [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? teamName = null,
            [FromQuery] string? departmentName = null,
            [FromQuery] List<Guid>? selectedWorkerIds = null)
        {

            try
            {
                var attendanceData = await _attendanceRepository.GetAllAttendancesAsync(
                startDate, endDate);

                attendanceData = ApplyFilters(attendanceData, teamName, departmentName, selectedWorkerIds);

                // Department Summary
                var departmentSummary = attendanceData
                    .GroupBy(a => a.Worker.DepartmentName ?? "Unknown")
                    .Select(g => new
                    {
                        Department = g.Key,
                        TotalWorkers = g.Select(a => a.WorkerId).Distinct().Count(),
                        TotalAttendance = g.Count(),
                        PresentCount = g.Count(a => a.Status == "Present"),
                        AbsentCount = g.Count(a => a.Status == "Absent"),
                        AttendanceRate = g.Any() ? ((double)g.Count(a => a.Status == "Present") / g.Count() * 100).ToString("F2") + "%" : "0%"
                    })
                    .OrderBy(d => d.Department)
                    .ToList();

                // Team Summary
                var teamSummary = attendanceData
                    .GroupBy(a => a.Worker.TeamName ?? "Unknown")
                    .Select(g => new
                    {
                        Team = g.Key,
                        Department = g.First().Worker.DepartmentName ?? "Unknown",
                        TotalWorkers = g.Select(a => a.WorkerId).Distinct().Count(),
                        TotalAttendance = g.Count(),
                        PresentCount = g.Count(a => a.Status == "Present"),
                        AttendanceRate = g.Any() ? ((double)g.Count(a => a.Status == "Present") / g.Count() * 100).ToString("F2") + "%" : "0%"
                    })
                    .OrderBy(t => t.Department)
                    .ThenBy(t => t.Team)
                    .ToList();

                // Worker Details
                var workerDetails = attendanceData
                    .GroupBy(a => a.Worker)
                    .Select(g => new
                    {
                        WorkerNumber = g.Key.WorkerNumber,
                        Name = $"{g.Key.FirstName} {g.Key.LastName}",
                        Email = g.Key.Email,
                        Department = g.Key.DepartmentName,
                        Team = g.Key.TeamName,
                        TotalAttendance = g.Count(),
                        PresentCount = g.Count(a => a.Status == "Present"),
                        AbsentCount = g.Count(a => a.Status == "Absent"),
                        LateCount = g.Count(a => a.Status == "Late"),
                        AttendanceRate = g.Any() ? ((double)g.Count(a => a.Status == "Present") / g.Count() * 100).ToString("F2") + "%" : "0%"
                    })
                    .OrderBy(w => w.Name)
                    .ToList();

                var deptCsv = GenerateCsv(departmentSummary, "DEPARTMENT SUMMARY REPORT");
                var teamCsv = GenerateCsv(teamSummary, "TEAM SUMMARY REPORT");
                var workersCsv = GenerateCsv(workerDetails, "WORKER DETAILS REPORT");

                var combined = $"Department Summary\n{deptCsv}\n\nTeam Summary\n{teamCsv}\n\nWorker Details\n{workersCsv}";
                var filename = $"comprehensive_summary_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                return File(Encoding.UTF8.GetBytes(combined), "text/csv", filename);
            }
            catch (Exception ex)
            {
              _logger.LogError(ex, "Error generating comprehensive summary report");
                return BadRequest(new { Message = "An error occurred while generating the report." });
            }           
        }


        /// <summary>
        /// Get filtered worker data for display (no download) with pagination
        /// </summary>
        [HttpGet("worker-data")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWorkerData(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? teamName = null,
            [FromQuery] string? departmentName = null,
            [FromQuery] List<Guid>? selectedWorkerIds = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = "LastName",
            [FromQuery] string? sortOrder = "asc")
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100); // Limit page size to 100 for performance

            var attendanceData = await _attendanceRepository.GetAllAttendancesAsync(startDate, endDate);


            // Apply filters
            attendanceData = ApplyFilters(attendanceData, teamName, departmentName, selectedWorkerIds);

            var workerIds = attendanceData.Select(a => a.WorkerId).Distinct().ToList();
            var habitsData = await _habitRepository.GetHabitsByWorkerIdAsync(workerIds);

            if (startDate.HasValue && endDate.HasValue)
            {
                habitsData = habitsData
                    .Where(h => h.CompletedAt >= startDate && h.CompletedAt <= endDate)
                    .ToList();
            }

            // Get all workers data for sorting and pagination
            var allWorkers = attendanceData
                .GroupBy(a => a.Worker)
                .Select(g =>
                {
                    var worker = g.Key;
                    var workerHabits = habitsData.Where(h => h.WorkerId == worker.Id).ToList();

                    return new WorkerDataDto
                    {
                        WorkerNumber = worker.WorkerNumber,
                        FirstName = worker.FirstName,
                        LastName = worker.LastName,
                        Email = worker.Email,
                        Department = worker.DepartmentName,
                        Team = worker.TeamName,
                        TotalAttendance = g.Count(),
                        PresentCount = g.Count(a => a.Status == "Present"),
                        AbsentCount = g.Count(a => a.Status == "Absent"),
                        LateCount = g.Count(a => a.Status == "Late"),
                        HabitCount = workerHabits.Count,
                        LastAttendance = g.Max(a => a.CreatedAt),
                        IsSelected = selectedWorkerIds?.Contains(worker.Id) ?? false,
                        AttendanceRate = g.Any() ? ((double)g.Count(a => a.Status == "Present") / g.Count() * 100) : 0
                    };
                })
                .ToList();

            // Apply sorting
            var sortedWorkers = ApplySorting(allWorkers, sortBy, sortOrder);

            // Apply pagination
            var totalCount = sortedWorkers.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedWorkers = sortedWorkers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Create response with pagination metadata
            var result = new PaginatedWorkerResponse
            {
                Workers = pagedWorkers,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPrevious = page > 1,
                HasNext = page < totalPages
            };

            return Ok(result);
        }

        #region DTOs
        public class AttendanceHabitRecordDto
        {
            public string WorkerNumber { get; set; } = string.Empty;
            public string WorkerName { get; set; } = string.Empty;
            public string Date { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Department { get; set; } = string.Empty;
            public string Team { get; set; } = string.Empty;
            public string Details { get; set; } = string.Empty;
            public string HabitName { get; set; } = string.Empty;
            public string HabitCompletedAt { get; set; } = string.Empty;
        }
        public class PaginatedWorkerResponse
        {
            public List<WorkerDataDto> Workers { get; set; } = new();
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public int TotalPages { get; set; }
            public bool HasPrevious { get; set; }
            public bool HasNext { get; set; }
        }
        public class DownloadRequest
        {
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string? TeamName { get; set; }
            public string? DepartmentName { get; set; }
            public List<Guid>? SelectedWorkerIds { get; set; }
        }

        public class WorkerDataDto
        {
            public Guid WorkerId { get; set; }
            public string WorkerNumber { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Department { get; set; } = string.Empty;
            public string Team { get; set; } = string.Empty;
            public int TotalAttendance { get; set; }
            public int PresentCount { get; set; }
            public int AbsentCount { get; set; }
            public int LateCount { get; set; }
            public int HabitCount { get; set; }
            public DateTime LastAttendance { get; set; }
            public bool IsSelected { get; set; }
            public double AttendanceRate { get; set; }
        }

        #endregion

        #region reports helpers

        private List<WorkerDataDto> ApplySorting(List<WorkerDataDto> workers, string? sortBy, string? sortOrder)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return workers.OrderBy(w => w.LastName).ThenBy(w => w.FirstName).ToList();

            var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "workernumber" => isDescending ?
                    workers.OrderByDescending(w => w.WorkerNumber).ToList() :
                    workers.OrderBy(w => w.WorkerNumber).ToList(),

                "firstname" => isDescending ?
                    workers.OrderByDescending(w => w.FirstName).ToList() :
                    workers.OrderBy(w => w.FirstName).ToList(),

                "lastname" => isDescending ?
                    workers.OrderByDescending(w => w.LastName).ToList() :
                    workers.OrderBy(w => w.LastName).ToList(),

                "email" => isDescending ?
                    workers.OrderByDescending(w => w.Email).ToList() :
                    workers.OrderBy(w => w.Email).ToList(),

                "department" => isDescending ?
                    workers.OrderByDescending(w => w.Department).ToList() :
                    workers.OrderBy(w => w.Department).ToList(),

                "team" => isDescending ?
                    workers.OrderByDescending(w => w.Team).ToList() :
                    workers.OrderBy(w => w.Team).ToList(),

                "totalattendance" => isDescending ?
                    workers.OrderByDescending(w => w.TotalAttendance).ToList() :
                    workers.OrderBy(w => w.TotalAttendance).ToList(),

                "presentcount" => isDescending ?
                    workers.OrderByDescending(w => w.PresentCount).ToList() :
                    workers.OrderBy(w => w.PresentCount).ToList(),

                "absentcount" => isDescending ?
                    workers.OrderByDescending(w => w.AbsentCount).ToList() :
                    workers.OrderBy(w => w.AbsentCount).ToList(),

                "latecount" => isDescending ?
                    workers.OrderByDescending(w => w.LateCount).ToList() :
                    workers.OrderBy(w => w.LateCount).ToList(),

                "habitcount" => isDescending ?
                    workers.OrderByDescending(w => w.HabitCount).ToList() :
                    workers.OrderBy(w => w.HabitCount).ToList(),

                "lastattendance" => isDescending ?
                    workers.OrderByDescending(w => w.LastAttendance).ToList() :
                    workers.OrderBy(w => w.LastAttendance).ToList(),

                "attendancerate" => isDescending ?
                    workers.OrderByDescending(w => w.AttendanceRate).ToList() :
                    workers.OrderBy(w => w.AttendanceRate).ToList(),

                _ => isDescending ?
                    workers.OrderByDescending(w => w.LastName).ThenByDescending(w => w.FirstName).ToList() :
                    workers.OrderBy(w => w.LastName).ThenBy(w => w.FirstName).ToList()
            };
        }
        private string GenerateCsv<T>(IEnumerable<T> data, string reportTitle)
        {
            if (!data.Any())
            {
                return $"{reportTitle}\nNo data available for the selected criteria.\n";
            }


            var csv = new StringBuilder();
            csv.AppendLine(reportTitle);
            csv.AppendLine();
            csv.AppendLine();

            var properties = typeof(T).GetProperties();
            // Add header row
            csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));

            // Add data rows
            foreach (var item in data)
            {
                var row = properties.Select(p =>
                {
                    var value = p.GetValue(item)?.ToString() ?? "";
                    // Escape CSV special characters
                    if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
                    {
                        value = $"\"{value.Replace("\"", "\"\"")}\"";
                    }
                    return value;
                });
                csv.AppendLine(string.Join(",", row));
            }

            return csv.ToString();
        }

        private List<AttendanceDto> ApplyFilters(IEnumerable<AttendanceDto> attendanceData, string? teamName, string? departmentName, List<Guid>? selectedWorkerIds)
        {
            // Filter by department
            if (!string.IsNullOrWhiteSpace(departmentName))
            {
                departmentName = departmentName.Trim().ToLower();
                attendanceData = attendanceData
                    .Where(a => a.Worker.DepartmentId?.ToLower() == departmentName)
                    .ToList();
            }

            // Filter by team
            if (!string.IsNullOrWhiteSpace(teamName))
            {
                teamName = teamName.Trim().ToLower();
                attendanceData = attendanceData
                    .Where(a => a.Worker.TeamName?.ToLower() == teamName)
                    .ToList();
            }

            // Filter by selected workers
            if (selectedWorkerIds != null && selectedWorkerIds.Any())
            {
                attendanceData = attendanceData
                    .Where(a => selectedWorkerIds.Contains(a.WorkerId))
                    .ToList();
            }

           return attendanceData.ToList();
        }
        #endregion
    }
}
