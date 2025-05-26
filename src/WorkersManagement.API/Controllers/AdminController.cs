using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Core.Repositories;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Dtos.Workers;
using WorkersManagement.Domain.Interfaces;

namespace WorkersManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        public async Task<IActionResult> CreateWorker([FromBody] CreateNewWorkerDto dto)
        {
            try
            {
                var worker = await _workersRepository.CreateWorkerAsync(dto);
                _logger.LogInformation("Worker created successfully with email: {Email}", dto.Email);
                return Ok(worker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating worker with email {Email}", dto.Email);
                return StatusCode(500, "An error occurred while creating the worker.");
            }
        }

        [HttpDelete("delete-worker/{id}")]
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
        public async Task<IActionResult> GetAllWorkers()
        {
            try
            {
                var workers = await _workersRepository.GetAllWorkersAsync();
                _logger.LogInformation("Fetched {Count} workers successfully", workers.Count());
                return Ok(workers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all workers.");
                return StatusCode(500, "An error occurred while retrieving the workers.");
            }
        }

        [HttpGet("workers/{id}")]
        public async Task<IActionResult> GetWorkerById(Guid id)
        {
            try
            {
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
        public async Task<IActionResult> UpdateWorkerAsync(Guid id, [FromBody] UpdateWorkersDto request)
        {
            try
            {
                var worker = await _workersRepository.GetWorkerByIdAsync(id);
                if (worker == null)
                    return NotFound("Worker not found.");

                // Lookup department by name
                var department = await _departmentRepository.GetDepartmentByNameAsync(request.DepartmentName);
                if (department == null)
                    return BadRequest("Specified department does not exist or is not assigned to a team.");

                // Update fields
                worker.FirstName = request.FirstName?.Trim();
                worker.LastName = request.LastName?.Trim();
                worker.Role = request.Role;
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
