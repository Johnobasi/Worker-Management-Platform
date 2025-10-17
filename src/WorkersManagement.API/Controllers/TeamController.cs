using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;

namespace WorkersManagement.API.Controllers
{
    /// <summary>
    /// Manage team operations and organization structure
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
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

        /// <summary>
        /// Create a new team
        /// </summary>
        /// <param name="request">Team creation data</param>
        /// <returns>Newly created team</returns>
        [HttpPost("create-team")]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { Errors = errors });
                }

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

        /// <summary>
        /// Get all teams with department information
        /// </summary>
        /// <returns>List of all teams</returns>
        [HttpGet("get-teams")]
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
        /// <summary>
        /// Get team details by name
        /// </summary>
        /// <param name="teamName">Team name</param>
        /// <returns>Team information</returns>
        [HttpGet("get-teams/{teamName}")]
        public async Task<IActionResult> GetTeamByIdAsync(string teamName)
        {
            try
            {
                var team = await _teamRepository.GetTeamByNameAsync(teamName);
                if (team == null)
                    return NotFound($"Team with Name '{teamName}' not found.");

                return Ok(team);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving team with Name {Name}", teamName);
                return StatusCode(500, "An error occurred while retrieving the team.");
            }
        }
        /// <summary>
        /// Update team information
        /// </summary>
        /// <param name="id">Team identifier</param>
        /// <param name="updatedTeam">Updated team data</param>
        /// <returns>Update result</returns>
        [HttpPut("update-team/{id}")]
        public async Task<IActionResult> UpdateTeamAsync(Guid id, [FromBody] UpdateTeamDto updatedTeam)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { Errors = errors });
                }

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

        /// <summary>
        /// Delete a team
        /// </summary>
        /// <param name="id">Team identifier</param>
        /// <returns>Delete result</returns>
        [HttpDelete("delete-team/{id}")]
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
