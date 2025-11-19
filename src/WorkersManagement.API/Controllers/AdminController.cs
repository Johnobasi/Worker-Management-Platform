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
    /// <summary>
    /// Administrative endpoints for worker management operations
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [Authorize]
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


        /// <summary>
        /// Create a new worker
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/admin/create-worker
        ///     FormData:
        ///     - Email: "john.doe@company.com"
        ///     - FirstName: "John"
        ///     - LastName: "Doe"
        ///     - DepartmentName: "Engineering"
        ///     - Role: ["Worker", "TeamLead"]
        ///     - ProfilePicture: [file]
        ///
        /// </remarks>
        /// <param name="dto">Worker creation data</param>
        /// <returns>Newly created worker details</returns>
        /// <response code="201">Worker created successfully</response>
        /// <response code="400">Invalid input data or department not found</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="403">Forbidden - Insufficient permissions</response>
        /// <response code="500">Internal server error</response>
        //[Authorize(Policy = "CanCreateWorkers")]
        [HttpPost("create-worker")]
        [AllowAnonymous]
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
                return CreatedAtAction(nameof(GetWorkerById), new { id = worker }, worker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating worker with email {Email}", dto.Email);
                return StatusCode(500, "An error occurred while creating the worker.");
            }
        }


        /// <summary>
        /// Create bulk workers from Excel file upload
        /// </summary>
        /// <remarks>
        /// Sample Excel format:
        /// 
        /// | Email | FirstName | LastName | DepartmentName | Role |
        /// |-------|-----------|----------|----------------|------|
        /// | john@email.com | John | Doe | Engineering | Worker,TeamLead |
        /// | jane@email.com | Jane | Smith | Marketing | Worker |
        /// 
        /// Required columns: Email, FirstName, LastName, DepartmentName, Role
        /// </remarks>
        /// <param name="file">Excel file (.xlsx) containing worker data</param>
        /// <returns>Bulk upload results with success/failure details</returns>
        /// <response code="200">Bulk upload completed with results</response>
        /// <response code="400">Invalid file or no data found</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="403">Forbidden - Insufficient permissions</response>
        /// <response code="500">Internal server error</response>
        //[Authorize(Policy = "CanCreateWorkers")]
        [HttpPost("upload-workers")]
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
                            WorkerId = worker
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

        /// <summary>
        /// Delete a worker by ID
        /// </summary>
        /// <param name="id">Worker ID</param>
        /// <returns>Success message</returns>
        /// <response code="200">Worker deleted successfully</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="403">Forbidden - SuperAdmin role required</response>
        /// <response code="404">Worker not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("delete-worker/{id}")]
        [AllowAnonymous]
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

        /// <summary>
        /// Get paginated list of all workers
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
        /// <returns>Paginated list of workers</returns>
        /// <response code="200">Workers retrieved successfully</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="500">Internal server error</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-workers")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetAllWorkers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 20;

                var workers = await _workersRepository.GetAllWorkersAsync();
                var totalCount = workers.Count;

                var pagedWorkers = workers
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var workerDtos = pagedWorkers.ToSummaryDtos();
                var response = new
                {
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Data = workerDtos
                };

                _logger.LogInformation("Fetched {Count} workers successfully", workerDtos.Count());
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all workers.");
                return StatusCode(500, "An error occurred while retrieving the workers.");
            }
        }

        /// <summary>
        /// Get total number of workers
        /// </summary>
        /// <returns>Total workers count</returns>
        /// <response code="200">Count retrieved successfully</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="500">Internal server error</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("count-workers")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetWorkersCount()
        {
            try
            {
                var workers = await _workersRepository.GetAllWorkersAsync();
                var totalCount = workers.Count;

                _logger.LogInformation("Total workers count retrieved: {Count}", totalCount);

                return Ok(new
                {
                    TotalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving workers count.");
                return StatusCode(500, "An error occurred while retrieving the workers count.");
            }
        }

        /// <summary>
        /// Get worker by ID
        /// </summary>
        /// <param name="id">Worker ID</param>
        /// <returns>Worker details</returns>
        /// <response code="200">Worker retrieved successfully</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="403">Forbidden - Workers can only view their own profile</response>
        /// <response code="404">Worker not found</response>
        /// <response code="500">Internal server error</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("workers/{id}")]
        [AllowAnonymous]
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

        /// <summary>
        /// Get email addresses of selected workers
        /// </summary>
        /// <remarks>
        /// Used for bulk email operations by providing worker IDs
        /// </remarks>
        /// <param name="request">List of worker IDs</param>
        /// <returns>List of email addresses</returns>
        /// <response code="200">Email addresses retrieved successfully</response>
        /// <response code="400">No worker IDs provided</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="404">No workers found with provided IDs</response>
        /// <response code="500">Internal server error</response>
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("selected-workers")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSelectedWorkers([FromBody] SelectedWorkersRequest request)
        {
            try
            {
                if (request?.SelectedWorkerIds == null || !request.SelectedWorkerIds.Any())
                {
                    return BadRequest("No worker IDs provided");
                }

                var allWorkers = await _workersRepository.GetAllWorkersAsync();
                var selectedWorkers = allWorkers
                    .Where(worker => request.SelectedWorkerIds.Contains(worker.Id))
                    .ToList();

                if (!selectedWorkers.Any())
                {
                    _logger.LogWarning("No workers found with the provided IDs");
                    return NotFound("No workers found with the provided IDs");
                }

                // Return only email addresses for recipient field
                var selectedEmails = selectedWorkers
                    .Select(worker => worker.Email)
                    .ToList();

                _logger.LogInformation("Fetched {Count} selected workers successfully", selectedEmails.Count);
                return Ok(selectedEmails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving selected workers");
                return StatusCode(500, "An error occurred while retrieving the selected workers.");
            }
        }

        /// <summary>
        /// Request for selected workers operation
        /// </summary>
        public class SelectedWorkersRequest
        {
            /// <summary>
            /// List of worker IDs to retrieve
            /// </summary>
            /// <example>["3fa85f64-5717-4562-b3fc-2c963f66afa6", "4fa85f64-5717-4562-b3fc-2c963f66afa7"]</example>
            public List<Guid> SelectedWorkerIds { get; set; } = new List<Guid>();
        }

        /// <summary>
        /// Update worker information
        /// </summary>
        /// <param name="id">Worker ID</param>
        /// <param name="request">Updated worker data</param>
        /// <returns>Success message</returns>
        /// <response code="200">Worker updated successfully</response>
        /// <response code="400">Invalid input data or department not found</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="403">Forbidden - Insufficient permissions or department access</response>
        /// <response code="404">Worker not found</response>
        /// <response code="500">Internal server error</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("update-worker/{id}")]
        [AllowAnonymous]
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
