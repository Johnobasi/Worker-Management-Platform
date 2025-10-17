using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.API.Controllers
{
    /// <summary>
    /// Manage departments and their associations with teams
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly ILogger<DepartmentController> _logger;
        private readonly ISubTeamRepository _subTeamRepository;
        public DepartmentController(IDepartmentRepository departmentRepository, ITeamRepository teamRepository,
            ILogger<DepartmentController> logger, ISubTeamRepository subTeamRepository)
        {
            _departmentRepository = departmentRepository;
            _teamRepository = teamRepository;
            _logger = logger;
            _subTeamRepository = subTeamRepository;
        }

        /// <summary>
        /// Get all departments with team information
        /// </summary>
        /// <returns>List of all departments</returns>
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
                    SubTeamName = d.Subteams?.Name,
                    Workers = d.Workers?.Select(u => u.FirstName).ToList() ?? new List<string>() 
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all departments.");
                return StatusCode(500, "An error occurred while retrieving departments.");
            }
        }

        /// <summary>
        /// Create a new department
        /// </summary>
        /// <param name="req">Department creation data</param>
        /// <returns>Newly created department</returns>
        [HttpPost("add-department")]
        public async Task<IActionResult> AddDepartment([FromBody] CreateDepartmenDto req)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { Errors = errors });
                }

                if (string.IsNullOrWhiteSpace(req.Name))
                    return BadRequest("Department name is required.");

                if (string.IsNullOrWhiteSpace(req.Description))
                    return BadRequest("Department description is required.");

                if (string.IsNullOrWhiteSpace(req.TeamName) && string.IsNullOrWhiteSpace(req.SubTeamName))
                    return BadRequest("Either a Team or SubTeam must be specified.");


                Department department = new()
                {
                    Id = Guid.NewGuid(),
                    Name = req.Name.Trim(),
                    Description = req.Description.Trim()
                };

                if (!string.IsNullOrWhiteSpace(req.SubTeamName))
                {
                    var subTeam = await _subTeamRepository.GetSubTeamsByTeamNameAsync(req.SubTeamName.Trim());
                    if (subTeam == null)
                        return BadRequest("Specified SubTeam is invalid.");

                    var team = await _teamRepository.GetTeamByNameAsync(subTeam.Team.Name);
                    if (team == null)
                        return BadRequest("SubTeam's parent team not found.");

                    department.SubTeamId = subTeam.Id;
                    department.TeamId = subTeam.TeamId;
                    department.Subteams = subTeam;
                    department.Teams = team;
                }
                else
                {
                    var team = await _teamRepository.GetTeamByNameAsync(req.TeamName!.Trim());
                    if (team == null)
                        return BadRequest("Specified Team is invalid.");

                    department.TeamId = team.Id;
                    department.Teams = team;
                }
                var createdDepartment = await _departmentRepository.CreateDepartmentAsync(department);
                return Ok(createdDepartment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding department '{DepartmentName}'", req?.Name);
                return StatusCode(500, "An error occurred while creating the department.");
            }
           
        }

        /// <summary>
        /// Get department details by name
        /// </summary>
        /// <param name="departmentName">Department name</param>
        /// <returns>Department information</returns>
        [HttpGet("get-department/{departmentName}")]
        public async Task<IActionResult> GetDepartmentNameAsync(string departmentName)
        {
            try
            {
                var department = await _departmentRepository.GetDepartmentByNameAsync(departmentName);

                var result = new DepartmentSummaryDto
                {
                    Name = department!.Name,
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

        /// <summary>
        /// Update department information
        /// </summary>
        /// <param name="id">Department identifier</param>
        /// <param name="updatedDepartment">Updated department data</param>
        /// <returns>Update result</returns>
        [HttpPut("update-department/{id}")]
        public async Task<IActionResult> UpdateDepartmentAsync(Guid id, [FromBody] UpdateDepartmentDto updatedDepartment)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { Errors = errors });
                }

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

        /// <summary>
        /// Delete a department
        /// </summary>
        /// <param name="id">Department identifier</param>
        /// <returns>Delete result</returns>
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
