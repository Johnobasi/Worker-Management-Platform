using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Interfaces;

namespace WorkersManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(
            IAttendanceRepository attendanceRepository,
            ILogger<AttendanceController> logger)
        {
            _attendanceRepository = attendanceRepository;
            _logger = logger;
        }

        // POST: api/attendance/mark
        [HttpPost("mark-workers-attendance")]
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
        [HttpPost("save")]
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

        // GET: api/attendance/worker/{workerId}?startDate=yyyy-MM-dd
        [HttpGet("worker/{workerId}")]
        public async Task<IActionResult> GetWorkerAttendances(Guid workerId, [FromQuery] DateTime startDate)
        {
            _logger.LogInformation($"Fetching attendance records for worker {workerId} since {startDate:yyyy-MM-dd}...");
            try
            {
                var attendances = await _attendanceRepository.GetWorkerAttendances(workerId, startDate);
                return Ok(attendances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching worker attendances.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // GET: api/attendance?startDate=yyyy-MM-dd&endDate=yyyy-MM-dd
        [HttpGet]
        public async Task<IActionResult> GetAllAttendances([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
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
