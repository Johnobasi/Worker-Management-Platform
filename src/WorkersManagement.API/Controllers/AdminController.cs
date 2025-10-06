using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Core.DTOS;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Dtos.Workers;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IWorkerManagementRepository _workersRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly ILogger<AdminController> _logger;
        public AdminController(IWorkerManagementRepository workerRepository, IDepartmentRepository departmentRepository,ILogger<AdminController> logger)
        {
            _workersRepository = workerRepository;
            _departmentRepository = departmentRepository;
            _logger = logger;
        }

        [HttpPost("create-worker")]
        //[Authorize(Policy = "CanCreateWorkers")]
        public async Task<IActionResult> CreateWorker([FromForm] CreateNewWorkerDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { Errors = errors });
            }
            try
            {
                var department = await _departmentRepository.GetDepartmentByNameAsync(dto.DepartmentName);
                if (department == null)
                return BadRequest("Specified department does not exist.");
               
                var worker = await _workersRepository.CreateWorkerAsync(dto);
                _logger.LogInformation("Worker created successfully with email: {Email}", dto.Email);
                return CreatedAtAction(nameof(GetWorkerById), new { id = worker.Id }, worker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating worker with email {Email}", dto.Email);
                return StatusCode(500, "An error occurred while creating the worker.");
            }
        }

        [HttpPost("upload-workers")]
        //[Authorize(Policy = "CanCreateWorkers")]
        public async Task<IActionResult> UploadWorkers(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a valid Excel file.");

            var results = new List<object>();

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    return BadRequest("No worksheet found in the Excel file.");

                var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1); // skip header
                if (rows == null)
                    return BadRequest("No data rows found in Excel sheet.");

                foreach (var row in rows)
                {
                    try
                    {
                        var dto = new CreateNewWorkerDto
                        {
                            Email = row.Cell(1).GetString().Trim(),            // ✅ Column A: Email
                            FirstName = row.Cell(2).GetString().Trim(),        // ✅ Column B: FirstName
                            LastName = row.Cell(3).GetString().Trim(),         // ✅ Column C: LastName
                            DepartmentName = row.Cell(4).GetString().Trim(),   // ✅ Column D: DepartmentName
                            Role = [.. row.Cell(5).GetString()                 // ✅ Column E: Role
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(r => Enum.Parse<UserRole>(r.Trim(), true))],
                            ProfilePicture = null
                        };

                        var worker = await _workersRepository.CreateWorkerAsync(dto);
                        results.Add(new
                        {
                            Row = row.RowNumber(),
                            Email = dto.Email,
                            Status = "Created",
                            WorkerId = worker.Id
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error on row {Row}", row.RowNumber());
                        results.Add(new
                        {
                            Row = row.RowNumber(),
                            Error = ex.Message
                        });
                    }
                }

                return Ok(new
                {
                    Message = "Bulk worker upload completed.",
                    TotalProcessed = results.Count,
                    Results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing uploaded Excel file.");
                return StatusCode(500, "An error occurred while processing the Excel file.");
            }
        }

        [HttpDelete("delete-worker/{id}")]
        [Authorize(Policy = "SuperAdmin")]
        public async Task<IActionResult> DeleteWorker(Guid id)
        {
            try
            {
                await _workersRepository.DeleteWorkerAsync(id);

                _logger.LogInformation("Worker with ID {WorkerId} deleted successfully", id);
                return Ok("Worker deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting worker with ID {WorkerId}", id);
                return StatusCode(500, "An error occurred while deleting the worker.");
            }
        }

        [HttpGet("get-workers")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetAllWorkers()
        {
            try
            {
                var workers = await _workersRepository.GetAllWorkersAsync();
                workers.Count();
                var workerDtos = workers.ToSummaryDtos();
                _logger.LogInformation("Fetched {Count} workers successfully", workerDtos.Count());
                return Ok(workerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all workers.");
                return StatusCode(500, "An error occurred while retrieving the workers.");
            }
        }

        [HttpGet("workers/{id}")]
        [Authorize(Policy = "Worker")]
        public async Task<IActionResult> GetWorkerById(Guid id)
        {
            try
            {
                // Workers can only view their own profile
                if (User.IsInRole(UserRole.Worker.ToString()) && id.ToString() != User.FindFirst("WorkerId")?.Value)
                    return Forbid("Workers can only view their own profile.");

                var worker = await _workersRepository.GetWorkerByIdAsync(id);
                if (worker == null)
                {
                    _logger.LogWarning("Worker with ID {WorkerId} not found", id);
                    return NotFound("Worker not found.");
                }

                _logger.LogInformation("Worker with ID {WorkerId} retrieved successfully", id);
                return Ok(worker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving worker with ID {WorkerId}", id);
                return StatusCode(500, "An error occurred while retrieving the worker.");
            }
        }


        [HttpPut("update-worker/{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateWorkerAsync(Guid id, [FromBody] UpdateWorkersDto request)
        {

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { Errors = errors });
            }

            try
            {
                var worker = await _workersRepository.GetWorkerByIdAsync(id);
                if (worker == null)
                    return NotFound("Worker not found.");

                // Additional check for SubTeamLead and HOD
                if (User.IsInRole(UserRole.SubTeamLead.ToString()) || User.IsInRole(UserRole.HOD.ToString()))
                {
                    var departments = await _departmentRepository.GetDepartmentByNameAsync(request.DepartmentName);
                    if (departments == null)
                        return BadRequest("Specified department does not exist.");

                    var userDepartmentId = User.FindFirst("DepartmentId")?.Value;
                    var userTeamId = User.FindFirst("TeamId")?.Value;

                    if (User.IsInRole(UserRole.SubTeamLead.ToString()) && departments.TeamId.ToString() != userTeamId)
                        return Forbid("SubTeamLeads can only update workers in their own subteam's departments.");
                    if (User.IsInRole(UserRole.HOD.ToString()) && departments.Id.ToString() != userDepartmentId)
                        return Forbid("HODs can only update workers in their own department.");
                }

                // Lookup department by name
                var department = await _departmentRepository.GetDepartmentByNameAsync(request.DepartmentName);
                if (department == null)
                    return BadRequest("Specified department does not exist or is not assigned to a team.");

                // Update fields
                worker.FirstName = request.FirstName?.Trim();
                worker.LastName = request.LastName?.Trim();
                worker.Roles = request.Role.ToList();
                worker.Department = department;

                await _workersRepository.UpdateWorkerAsync(worker);

                return Ok("Worker updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating worker with ID {WorkerId}", id);
                return StatusCode(500, "An error occurred while updating the worker.");
            }
        }

    }
}
