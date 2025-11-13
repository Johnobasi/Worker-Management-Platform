using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.API.Controllers
{
    /// <summary>
    /// Manage worker attendance records and tracking
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly ILogger<AttendanceController> _logger;
        private readonly IWorkerManagementRepository _workersRepository;

        public AttendanceController(
            IAttendanceRepository attendanceRepository,
            ILogger<AttendanceController> logger,
            IWorkerManagementRepository workersRepository)
        {
            _attendanceRepository = attendanceRepository;
            _logger = logger;
            _workersRepository = workersRepository;
        }

        // POST: api/attendance/mark
        /// <summary>
        /// Mark attendance using QR code data
        /// </summary>
        /// <param name="qrCodeData">QR code content containing worker information</param>
        /// <returns>Attendance marking result</returns>
        [HttpPost("mark-workers-attendance")]
        [Authorize(Policy = "Worker")]
        public async Task<IActionResult> MarkAttendance([FromBody] string qrCodeData)
        {
            _logger.LogInformation("Received request to mark attendance...");
            try
            {
                var result = await _attendanceRepository.MarkAttendanceAsync(qrCodeData);
                return result
                    ? Ok("Attendance marked successfully.")
                    : BadRequest("Failed to mark attendance.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in marking attendance.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // POST: api/attendance/save
        /// <summary>
        /// Manually save attendance record
        /// </summary>
        /// <param name="workerId">Worker identifier</param>
        /// <param name="checkInTime">Check-in date and time</param>
        /// <returns>Save confirmation</returns>
        [HttpPost("save")]
        [Authorize(Policy = "Worker")]
        public async Task<IActionResult> SaveAttendance([FromQuery] Guid workerId, [FromQuery] DateTime checkInTime)
        {
            _logger.LogInformation($"Received manual attendance save request for worker {workerId}...");
            try
            {
                await _attendanceRepository.SaveAttendance(workerId, checkInTime);
                return Ok("Attendance saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving manual attendance.");
                return StatusCode(500, "Internal server error.");
            }
        }

        /// <summary>
        /// Get attendance records for a specific worker
        /// </summary>
        /// <param name="workerId">Worker identifier</param>
        /// <param name="startDate">Start date for filtering records</param>
        /// <returns>List of attendance records</returns>
        [HttpGet("worker/{workerId}")]
        [Authorize(Policy = "Worker")]
        public async Task<IActionResult> GetWorkerAttendances(Guid workerId, [FromQuery] DateTime startDate)
        {
            _logger.LogInformation($"Fetching attendance records for worker {workerId} since {startDate:yyyy-MM-dd}...");
            try
            {
                // Workers can only view their own attendance
                if (User.IsInRole(UserRole.Worker.ToString()) && workerId.ToString() != User.FindFirst("WorkerId")?.Value)
                    return Forbid("Workers can only view their own attendance records.");

                // HODs can only view attendance for workers in their department
                if (User.IsInRole(UserRole.HOD.ToString()))
                {
                    var worker = await _workersRepository.GetWorkerByIdAsync(workerId);
                    var userDepartmentId = User.FindFirst("DepartmentId")?.Value;
                    if (worker?.Department?.Id.ToString() != userDepartmentId)
                        return Forbid("HODs can only view attendance for workers in their own department.");
                }

                var attendances = await _attendanceRepository.GetWorkerAttendances(workerId, startDate);
                return Ok(attendances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching worker attendances.");
                return StatusCode(500, "Internal server error.");
            }
        }

        /// <summary>
        /// Get all attendance records within date range (Admin only)
        /// </summary>
        /// <param name="startDate">Start date of range</param>
        /// <param name="endDate">End date of range</param>
        /// <returns>All attendance records in date range</returns>
        [HttpGet]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetAllAttendances([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            _logger.LogInformation($"Fetching all attendances between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}...");
            try
            {
                var attendances = await _attendanceRepository.GetAllAttendancesAsync(startDate, endDate);
                return Ok(attendances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attendance records.");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}
