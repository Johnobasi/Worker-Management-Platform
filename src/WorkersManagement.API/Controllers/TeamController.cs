using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TeamController : ControllerBase
    {
        private readonly ITeamRepository _teamRepository;
        private readonly ILogger<TeamController> _logger;

        public TeamController(
            IDepartmentRepository departmentRepository,
            ITeamRepository teamRepository,
            ILogger<TeamController> logger)
        {
            _teamRepository = teamRepository;
            _logger = logger;
        }

        [HttpPost("create-team")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest("Team name is required.");

                var team = new Team
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim()
                };

                var createdTeam = await _teamRepository.CreateTeamAsync(team);
                return Ok(createdTeam);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating team '{TeamName}'", request?.Name);
                return StatusCode(500, "An error occurred while creating the team.");
            }
        }

        [HttpGet("get-teams")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetTeamsAsync()
        {
            try
            {
                var allTeams = await _teamRepository.GetAllTeamsAsync();
                var resultTeams = allTeams.Select(t => new TeamDto
                {
                    Name = t.Name,
                    Description = t.Description,
                    DepartmentNames = t.Departments?.Select(d => d.Name).ToList()
                }).ToList();

                return Ok(resultTeams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all teams.");
                return StatusCode(500, "An error occurred while retrieving teams.");
            }
        }

        [HttpGet("get-teams/{teamName}")]
        [Authorize(Policy = "SubTeamLead")]
        public async Task<IActionResult> GetTeamByIdAsync(string teamName)
        {
            try
            {
                var team = await _teamRepository.GetTeamByNameAsync(teamName);
                if (team == null)
                    return NotFound($"Team with Name '{teamName}' not found.");

                // SubTeamLeads can only view their own team
                if (User.IsInRole(UserRole.SubTeamLead.ToString()) && team.Id.ToString() != User.FindFirst("TeamId")?.Value)
                    return Forbid("SubTeamLeads can only view their own team.");

                return Ok(team);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving team with Name {Name}", teamName);
                return StatusCode(500, "An error occurred while retrieving the team.");
            }
        }

        [HttpPut("update-team/{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UpdateTeamAsync(Guid id, [FromBody] UpdateTeamDto updatedTeam)
        {
            try
            {
                if (id != updatedTeam.Id)
                    return BadRequest("Team ID mismatch.");

                var result = await _teamRepository.UpdateTeamAsync(updatedTeam);
                if (!result)
                    return NotFound($"Team with ID '{id}' not found.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating team with ID {TeamId}", id);
                return StatusCode(500, "An error occurred while updating the team.");
            }
        }

        [HttpDelete("delete-team/{id}")]
        [Authorize(Policy = "SuperAdmin")]
        public async Task<IActionResult> DeleteTeamAsync(Guid id)
        {
            try
            {
                var result = await _teamRepository.DeleteTeamAsync(id);
                if (!result)
                    return NotFound($"Team with ID '{id}' not found.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting team with ID {TeamId}", id);
                return StatusCode(500, "An error occurred while deleting the team.");
            }
        }
    }
}
