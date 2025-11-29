using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using WorkersManagement.Domain.Dtos.Habits;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;

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
        private readonly WorkerDbContext _context;
        public ReportController(IHabitRepository habitRepository, IAttendanceRepository attendanceRepository,
            ILogger<ReportController> logger, WorkerDbContext workerDbContext)
        {
            _attendanceRepository = attendanceRepository;
            _habitRepository = habitRepository;
            _logger = logger;
            _context = workerDbContext;
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
                // Get ALL workers first
                var workersQuery = _context.Workers
                    .Include(w => w.Department)
                    .ThenInclude(d => d.Teams)
                    .AsQueryable();

                // Apply filters to workers query
                if (!string.IsNullOrWhiteSpace(departmentName))
                    workersQuery = workersQuery.Where(w =>
                        w.Department.Name != null &&
                        w.Department.Name.ToLower().Contains(departmentName.ToLower()));

                if (!string.IsNullOrWhiteSpace(teamName))
                    workersQuery = workersQuery.Where(w =>
                        w.Department.Teams.Name != null &&
                        w.Department.Teams.Name.ToLower().Contains(teamName.ToLower()));

                if (selectedWorkerIds != null && selectedWorkerIds.Any())
                    workersQuery = workersQuery.Where(w => selectedWorkerIds.Contains(w.Id));

                var allFilteredWorkers = await workersQuery.ToListAsync();
                var workerIds = allFilteredWorkers.Select(w => w.Id).ToList();

                // Get attendance and habits for ALL workers
                var attendanceData = await _attendanceRepository.GetAllAttendancesAsync(startDate, endDate);
                var habitsData = await _habitRepository.GetHabitsByWorkerIdAsync(workerIds);

                if (startDate.HasValue && endDate.HasValue)
                {
                    habitsData = habitsData
                        .Where(h => h.CompletedAt >= startDate && h.CompletedAt <= endDate)
                        .ToList();
                }
                // Group data for efficient lookups
                var attendanceByWorker = attendanceData
                    .Where(a => workerIds.Contains(a.WorkerId))
                    .GroupBy(a => a.WorkerId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var habitsByWorker = habitsData
                    .GroupBy(h => h.WorkerId)
                    .ToDictionary(g => g.Key, g => g.ToList());



                attendanceData = ApplyFilters(attendanceData, teamName, departmentName, selectedWorkerIds);

                var summaries = allFilteredWorkers.Select(worker =>
                {
                    var workerAttendances = attendanceData.Where(a => a.WorkerId == worker.Id).ToList();
                    var workerHabits = habitsData.Where(h => h.WorkerId == worker.Id).ToList();
                    int total = workerAttendances.Count;
                    return new
                    {
                        WorkerNumber = worker.WorkerNumber,
                        Name = $"{worker.FirstName} {worker.LastName}",
                        Email = worker.Email,
                        Department = worker.Department?.Name ?? "N/A",
                        Team = worker.Department?.Teams?.Name ?? "N/A",
                        AttendanceCount = workerAttendances.Count,
                        PresentCount = workerAttendances.Count(a => a.Status == "Present"),
                        AbsentCount = workerAttendances.Count(a => a.Status == "Absent"),
                        LateCount = workerAttendances.Count(a => a.Status == "Late"),
                        HabitCount = workerHabits.Count,
                        AttendanceRate = workerAttendances.Any()
                            ? ((double)workerAttendances.Count(a => a.Status == "Present") / workerAttendances.Count * 100).ToString("F2") + "%"
                            : "0%"
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
                // 1. Fetch filtered workers
                var workersQuery = _context.Workers
                    .Include(w => w.Department)
                    .ThenInclude(d => d.Teams)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(departmentName))
                    workersQuery = workersQuery.Where(w => w.Department.Name != null &&
                                                           w.Department.Name.ToLower().Contains(departmentName.ToLower()));

                if (!string.IsNullOrWhiteSpace(teamName))
                    workersQuery = workersQuery.Where(w => w.Department.Teams.Name != null &&
                                                           w.Department.Teams.Name.ToLower().Contains(teamName.ToLower()));

                if (selectedWorkerIds != null && selectedWorkerIds.Any())
                    workersQuery = workersQuery.Where(w => selectedWorkerIds.Contains(w.Id));

                var filteredWorkers = await workersQuery.ToListAsync();
                var workerIds = filteredWorkers.Select(w => w.Id).ToList();

                // 2. Fetch attendance and habits for filtered workers
                var attendanceData = (await _attendanceRepository.GetAllAttendancesAsync(startDate, endDate))
                                     .Where(a => workerIds.Contains(a.WorkerId))
                                     .ToList();

                var habitsData = (await _habitRepository.GetHabitsByWorkerIdAsync(workerIds))
                                 .Where(h => !startDate.HasValue || h.CompletedAt >= startDate)
                                 .Where(h => !endDate.HasValue || h.CompletedAt <= endDate)
                                 .ToList();

                // 3. Build one summary row per worker
                var reportData = filteredWorkers.Select(worker =>
                {
                    var workerAttendances = attendanceData.Where(a => a.WorkerId == worker.Id).ToList();
                    var workerHabits = habitsData.Where(h => h.WorkerId == worker.Id).ToList();

                    int totalAttendance = workerAttendances.Count;
                    int presentCount = workerAttendances.Count(a => a.Status == "Present");
                    int absentCount = workerAttendances.Count(a => a.Status == "Absent");
                    int lateCount = workerAttendances.Count(a => a.Status == "Late");
                    int habitCount = workerHabits.Count;

                    var lastAttendance = workerAttendances.Any()
                        ? workerAttendances.Max(a => a.CreatedAt)
                        : (DateTime?)null;

                    return new AttendanceHabitRecordDto
                    {
                        WorkerNumber = worker.WorkerNumber,
                        WorkerName = $"{worker.FirstName} {worker.LastName}",
                        Date = lastAttendance?.ToString("yyyy-MM-dd") ?? "",
                        Type = "Summary",
                        Status = totalAttendance > 0 ? "Has Records" : "No Records",
                        Department = worker.Department?.Name ?? "N/A",
                        Team = worker.Department?.Teams?.Name ?? "N/A",
                        Details = $"Total Attendance: {totalAttendance}, Present: {presentCount}, Absent: {absentCount}, Late: {lateCount}",
                        HabitName = $"Total Habits: {habitCount}",
                        HabitCompletedAt = workerHabits.Any()
                            ? workerHabits.Max(h => h.CompletedAt).ToString("yyyy-MM-dd HH:mm")
                            : ""
                    };
                }).ToList();

                // 4. Generate CSV
                var csv = GenerateCsv(reportData, "DETAILED ATTENDANCE REPORT");
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
                      // Get ALL workers first
                    var workersQuery = _context.Workers
                        .Include(w => w.Department)
                        .ThenInclude(d => d.Teams)
                        .AsQueryable();

                // Apply filters to workers query
                if (!string.IsNullOrWhiteSpace(departmentName))
                    workersQuery = workersQuery.Where(w =>
                        w.Department.Name != null &&
                        w.Department.Name.ToLower().Contains(departmentName.ToLower()));

                if (!string.IsNullOrWhiteSpace(teamName))
                    workersQuery = workersQuery.Where(w =>
                        w.Department.Teams.Name != null &&
                        w.Department.Teams.Name.ToLower().Contains(teamName.ToLower()));

                if (selectedWorkerIds != null && selectedWorkerIds.Any())
                    workersQuery = workersQuery.Where(w => selectedWorkerIds.Contains(w.Id));

                var allFilteredWorkers = await workersQuery.ToListAsync();
                var workerIds = allFilteredWorkers.Select(w => w.Id).ToList();

                // Get attendance and habits for ALL workers
                var attendanceData = await _attendanceRepository.GetAllAttendancesAsync(startDate, endDate);
                var habitsData = await _habitRepository.GetHabitsByWorkerIdAsync(workerIds);

                if (startDate.HasValue && endDate.HasValue)
                {
                    habitsData = habitsData
                        .Where(h => h.CompletedAt >= startDate && h.CompletedAt <= endDate)
                        .ToList();
                }

                // Group data for efficient lookups
                var attendanceByWorker = attendanceData
                    .Where(a => workerIds.Contains(a.WorkerId))
                    .GroupBy(a => a.WorkerId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Department Summary - Include ALL departments from filtered workers
                var departmentSummary = allFilteredWorkers
                    .GroupBy(w => w.Department?.Name ?? "Unknown")
                    .Select(g =>
                    {
                        var departmentWorkers = g.ToList();
                        var departmentWorkerIds = departmentWorkers.Select(w => w.Id).ToList();
                        var deptAttendances = attendanceData
                            .Where(a => departmentWorkerIds.Contains(a.WorkerId))
                            .ToList();

                        return new
                        {
                            Department = g.Key,
                            TotalWorkers = departmentWorkers.Count,
                            TotalAttendance = deptAttendances.Count,
                            PresentCount = deptAttendances.Count(a => a.Status == "Present"),
                            AbsentCount = deptAttendances.Count(a => a.Status == "Absent"),
                            AttendanceRate = deptAttendances.Any()
                                ? ((double)deptAttendances.Count(a => a.Status == "Present") / deptAttendances.Count * 100).ToString("F2") + "%"
                                : "0%"
                        };
                    })
                    .OrderBy(d => d.Department)
                    .ToList();

                // Team Summary - Include ALL teams from filtered workers
                var teamSummary = allFilteredWorkers
                    .GroupBy(w => w.Department?.Teams?.Name ?? "Unknown")
                    .Select(g =>
                    {
                        var teamWorkers = g.ToList();
                        var teamWorkerIds = teamWorkers.Select(w => w.Id).ToList();
                        var teamAttendances = attendanceData
                            .Where(a => teamWorkerIds.Contains(a.WorkerId))
                            .ToList();

                        return new
                        {
                            Team = g.Key,
                            Department = g.First().Department?.Name ?? "Unknown",
                            TotalWorkers = teamWorkers.Count,
                            TotalAttendance = teamAttendances.Count,
                            PresentCount = teamAttendances.Count(a => a.Status == "Present"),
                            AttendanceRate = teamAttendances.Any()
                                ? ((double)teamAttendances.Count(a => a.Status == "Present") / teamAttendances.Count * 100).ToString("F2") + "%"
                                : "0%"
                        };
                    })
                    .OrderBy(t => t.Department)
                    .ThenBy(t => t.Team)
                    .ToList();

                // Worker Details - Include ALL workers
                var workerDetails = allFilteredWorkers.Select(worker =>
                {

                    var workerAttendances = attendanceData
                       .Where(a => a.WorkerId == worker.Id)
                       .ToList();

                    var workerHabits = habitsData
                        .Where(h => h.WorkerId == worker.Id)
                        .ToList();

                    return new
                    {
                        WorkerNumber = worker.WorkerNumber,
                        Name = $"{worker.FirstName} {worker.LastName}",
                        Email = worker.Email,
                        Department = worker.Department?.Name ?? "N/A",
                        Team = worker.Department?.Teams?.Name ?? "N/A",
                        TotalAttendance = workerAttendances.Count,
                        PresentCount = workerAttendances.Count(a => a.Status == "Present"),
                        AbsentCount = workerAttendances.Count(a => a.Status == "Absent"),
                        LateCount = workerAttendances.Count(a => a.Status == "Late"),
                        HabitCount = workerHabits.Count,
                        AttendanceRate = workerAttendances.Any()
                            ? ((double)workerAttendances.Count(a => a.Status == "Present") / workerAttendances.Count * 100).ToString("F2") + "%"
                            : "0%"
                    };
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

            // Build workers query
            var workersQuery = _context.Workers
                .Include(w => w.Department)
                .ThenInclude(d => d.Teams)
                .AsQueryable();

            // Apply filters to workers query
            if (!string.IsNullOrWhiteSpace(departmentName))
                workersQuery = workersQuery.Where(w =>
                    w.Department.Name != null &&
                    w.Department.Name.ToLower().Contains(departmentName.ToLower()));

            if (!string.IsNullOrWhiteSpace(teamName))
                workersQuery = workersQuery.Where(w =>
                    w.Department.Teams.Name != null &&
                    w.Department.Teams.Name.ToLower().Contains(teamName.ToLower()));

            if (selectedWorkerIds != null && selectedWorkerIds.Any())
                workersQuery = workersQuery.Where(w => selectedWorkerIds.Contains(w.Id));

            // Get all filtered workers
            var allFilteredWorkers = await workersQuery.ToListAsync();
            var workerIds = allFilteredWorkers.Select(w => w.Id).ToList();

            // Fetch attendance and habit data for all filtered workers
            var attendanceData = await _attendanceRepository.GetAllAttendancesAsync(startDate, endDate);
            var habitsData = await _habitRepository.GetHabitsByWorkerIdAsync(workerIds);

            // Filter habits by date range if provided
            if (startDate.HasValue && endDate.HasValue)
            {
                habitsData = habitsData
                    .Where(h => h.CompletedAt >= startDate && h.CompletedAt <= endDate)
                    .ToList();
            }

            // Group data for efficient lookups
            var attendanceByWorker = attendanceData
                .Where(a => workerIds.Contains(a.WorkerId))
                .GroupBy(a => a.WorkerId)
                .ToDictionary(g => g.Key, g => g.ToList());


            var habitsByWorker = habitsData
                .GroupBy(h => h.WorkerId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Build worker data for ALL filtered workers (including those with no attendance)
            var allWorkers = allFilteredWorkers.Select(worker =>
            {
                var workerAttendances = attendanceData.Where(a => a.WorkerId == worker.Id).ToList();
                var workerHabits = habitsData.Where(h => h.WorkerId == worker.Id).ToList();
                int total = workerAttendances.Count;
                return new WorkerDataDto
                {
                    WorkerNumber = worker.WorkerNumber,
                    FirstName = worker.FirstName,
                    LastName = worker.LastName,
                    Email = worker.Email,
                    Department = worker.Department?.Name ?? "N/A",
                    Team = worker.Department?.Teams?.Name ?? "N/A",
                    TotalAttendance = total,
                    PresentCount = workerAttendances.Count(a => a.Status == "Present"),
                    AbsentCount = workerAttendances.Count(a => a.Status == "Absent"),
                    LateCount = workerAttendances.Count(a => a.Status == "Late"),
                    HabitCount = workerHabits.Count,
                    LastAttendance = workerAttendances.Any()
                    ? workerAttendances.Max(a => a.CreatedAt)
                    : DateTime.MinValue,
                    IsSelected = selectedWorkerIds?.Contains(worker.Id) ?? false,
                    AttendanceRate = workerAttendances.Any()
                        ? Math.Round((double)workerAttendances.Count(a => a.Status == "Present") / workerAttendances.Count * 100, 2)
                        : 0
                };
            }).ToList();

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
