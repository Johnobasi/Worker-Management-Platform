using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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

        [HttpGet("all-departments")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetAllDepartmentsAsync()
        {
            try
            {
                // Manual role check against UserRole enum
                var userRoles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                bool hasRequiredRole = userRoles.Any(role =>
                    role == UserRole.Admin.ToString() ||
                    role == UserRole.SuperAdmin.ToString());

                if (!hasRequiredRole)
                {
                    _logger.LogWarning("User with roles {Roles} attempted to fetch department but lacks required role (Admin or SuperAdmin).", string.Join(", ", userRoles));
                    return Forbid("User does not have the required role to fetch department data.");
                }

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
    

        [HttpPost("add-department")]
       [Authorize(Policy = "SubTeamLead")]
        public async Task<IActionResult> AddDepartment([FromBody] CreateDepartmenDto req)
        {
            try
            {
                // Manual role check against UserRole enum
                var userRoles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                bool hasRequiredRole = userRoles.Any(role =>
                    role == UserRole.Admin.ToString() ||
                    role == UserRole.SuperAdmin.ToString());

                if (!hasRequiredRole)
                {
                    _logger.LogWarning("User with roles {Roles} attempted to create a new department but lacks required role (Admin or SuperAdmin).", string.Join(", ", userRoles));
                    return Forbid("User does not have the required role to create a department.");
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

                // SubTeamLead can only create within their own team
                if (User.IsInRole(UserRole.SubTeamLead.ToString()) &&
                    department.TeamId.ToString() != User.FindFirst("TeamId")?.Value)
                {
                    return Forbid("SubTeamLeads can only create departments in their own team.");
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

        [HttpGet("get-department/{departmentName}")]
        [Authorize(Policy = "HOD")]
        public async Task<IActionResult> GetDepartmentNameAsync(string departmentName)
        {
            try
            {
                // Manual role check against UserRole enum
                var userRoles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                bool hasRequiredRole = userRoles.Any(role =>
                    role == UserRole.HOD.ToString());

                if (!hasRequiredRole)
                {
                    _logger.LogWarning("User with roles {Roles} attempted to fetch deaprtment but lacks required role (Admin or SuperAdmin).", string.Join(", ", userRoles));
                    return Forbid("User does not have the required role to fetch department data.");
                }

                var department = await _departmentRepository.GetDepartmentByNameAsync(departmentName);

                // HODs can only view their own department
                if (User.IsInRole(UserRole.HOD.ToString()) && department?.Id.ToString() != User.FindFirst("DepartmentId")?.Value)
                    return Forbid("HODs can only view their own department.");

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

        [HttpPut("update-department/{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateDepartmentAsync(Guid id, [FromBody] UpdateDepartmentDto updatedDepartment)
        {
            try
            {
                // Manual role check against UserRole enum
                var userRoles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                bool hasRequiredRole = userRoles.Any(role =>
                    role == UserRole.Admin.ToString() ||
                    role == UserRole.SuperAdmin.ToString());

                if (!hasRequiredRole)
                {
                    _logger.LogWarning("User with roles {Roles} attempted to update a department but lacks required role (Admin or SuperAdmin).", string.Join(", ", userRoles));
                    return Forbid("User does not have the required role to update deaprtment.");
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

        [HttpDelete("delete-department/{id}")]
        [Authorize(Policy = "SuperAdmin")]
        public async Task<IActionResult> DeleteDepartmentAsync(Guid id)
        {

            try
            {
                // Manual role check against UserRole enum
                var userRoles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                bool hasRequiredRole = userRoles.Any(role =>
                    role == UserRole.SuperAdmin.ToString());

                if (!hasRequiredRole)
                {
                    _logger.LogWarning("User with roles {Roles} attempted to delete department but lacks required role (Admin or SuperAdmin).", string.Join(", ", userRoles));
                    return Forbid("User does not have the required role to delete department.");
                }

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
