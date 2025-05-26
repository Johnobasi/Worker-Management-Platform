using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly ILogger<DepartmentController> _logger;
        public DepartmentController(IDepartmentRepository departmentRepository, ITeamRepository teamRepository, ILogger<DepartmentController> logger)
        {
            _departmentRepository = departmentRepository;
            _teamRepository = teamRepository;
            _logger = logger;
        }

        [HttpGet("all-departments")]
        public async Task<IActionResult> GetAllDepartmentsAsync()
        {
            try
            {
                var departments = await _departmentRepository.AllDepartmentsAsync();

                var result = departments.Select(d => new AllDepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    TeamName = d.Teams?.Name,
                    Users = d.Workers?.Select(u => u.FirstName).ToList() ?? new List<string>() 
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all departments.");
                return StatusCode(500, "An error occurred while retrieving departments.");
            }
        }
    

        [HttpPost("add-department")]
        public async Task<IActionResult> AddDepartment([FromBody] CreateDepartmenDto req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Name))
                    return BadRequest("Department name is required.");

                if (string.IsNullOrWhiteSpace(req.Description))
                    return BadRequest("Department description is required.");


                var availableTeams = await _teamRepository.GetAllTeamsAsync();
                var selectedTeam = availableTeams
                    .FirstOrDefault(t => string.Equals(t.Name.Trim(), req.TeamName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (selectedTeam == null)
                    return BadRequest("Selected team is invalid.");
                var department = new Department
                {
                    Id = Guid.NewGuid(),
                    Name = req.Name.Trim(),
                    Description = req.Description.Trim(),
                    TeamId = selectedTeam.Id,
                    Teams = selectedTeam
                };
                var createdDepartment = await _departmentRepository.CreateDepartmentAsync(department);
                return Ok(createdDepartment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding department '{DepartmentName}'", req?.Name);
                return StatusCode(500, "An error occurred while creating the department.");
            }
           
        }

        [HttpGet("get-department/{departmentName}")]
        public async Task<IActionResult> GetDepartmentNameAsync(string departmentName)
        {
            try
            {
                var department = await _departmentRepository.GetDepartmentByNameAsync(departmentName);

                var result = new DepartmentSummaryDto
                {
                    Name = department.Name,
                    Description = department.Description,
                    TeamName = department.Teams?.Name,
                    Users = department.Workers?.Select(u => u.FirstName).ToList() ?? new List<string>()
                };
                if (department == null)
                    return NotFound($"Department with Name '{departmentName}' not found.");

                return Ok(department);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching department with Name {DepartmentName}", departmentName);
                return StatusCode(500, "An error occurred while retrieving the department.");
         
            }

        }

        [HttpPut("update-department/{id}")]
        public async Task<IActionResult> UpdateDepartmentAsync(Guid id, [FromBody] UpdateDepartmentDto updatedDepartment)
        {
            try
            {
                if (id != updatedDepartment.Id)
                    return BadRequest("Department ID mismatch.");

                var result = await _departmentRepository.UpdateDepartmentAsync(updatedDepartment);
                if (!result)
                    return NotFound($"Department with ID '{id}' not found.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating department with ID {DepartmentId}", id);
                return StatusCode(500, "An error occurred while updating the department.");
            }

        }

        [HttpDelete("delete-department/{id}")]
        public async Task<IActionResult> DeleteDepartmentAsync(Guid id)
        {

            try
            {
                var result = await _departmentRepository.DeleteDepartmentAsync(id);
                if (!result)
                    return NotFound("Team not found.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting department with ID {DepartmentId}", id);
                return StatusCode(500, "An internal server error occurred.");
            }

        }
    }
}
