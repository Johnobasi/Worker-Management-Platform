using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Dtos.SubTeam;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.API.Controllers
{
    /// <summary>
    /// Manage subteam operations and associations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SubTeamController : ControllerBase
    {
        private readonly ISubTeamRepository _subTeamService;
        private readonly ILogger<SubTeamController> _logger;
        private readonly ITeamRepository _teamRepository;

        public SubTeamController(ISubTeamRepository subTeamService, ILogger<SubTeamController> logger, ITeamRepository teamRepository)
        {
            _subTeamService = subTeamService;
            _logger = logger;
            _teamRepository = teamRepository;
        }

        /// <summary>
        /// Create a new subteam
        /// </summary>
        /// <param name="subTeamCreateDto">Subteam creation data</param>
        /// <returns>Newly created subteam</returns>
        [HttpPost("add-new-subteam")]
        public async Task<ActionResult<SubTeamDto>> CreateSubTeam([FromBody] CreateSubTeamDto subTeamCreateDto)
        {
            _logger.LogInformation("Creating new subteam with Team Name: {Name}", subTeamCreateDto?.Name);
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { Errors = errors });
                }
                if (string.IsNullOrWhiteSpace(subTeamCreateDto!.Name))
                    return BadRequest("Department name is required.");

                if (string.IsNullOrWhiteSpace(subTeamCreateDto.Description))
                    return BadRequest("Department description is required.");


                var availableTeams = await _teamRepository.GetAllTeamsAsync();
                var selectedTeam = availableTeams
                    .FirstOrDefault(t => string.Equals(t.Name.Trim(), subTeamCreateDto.TeamName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (selectedTeam == null)
                    return BadRequest("Selected team is invalid.");

                var subteamRequest = new SubTeam
                {
                    Id = Guid.NewGuid(),
                    Name = subTeamCreateDto.Name,
                    Team = selectedTeam,
                    TeamId = selectedTeam.Id
                };

                var createdSubTeam = await _subTeamService.CreateSubTeamAsync(subteamRequest);
                _logger.LogInformation("Subteam created successfully with Id: {SubTeamId}", createdSubTeam.Name);
                return Ok(createdSubTeam);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Error creating subteam: {ErrorMessage}", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get subteam by ID
        /// </summary>
        /// <param name="id">Subteam identifier</param>
        /// <returns>Subteam details</returns>
        [HttpGet("get-subteam-by-Id/{id}")]
        public async Task<ActionResult<SubTeamDto>> GetSubTeam(Guid id)
        {
            _logger.LogInformation("Retrieving subteam with Id: {SubTeamId}", id);
            try
            {
                var subTeam = await _subTeamService.GetSubTeamByIdAsync(id);
                if (subTeam == null)
                {
                    _logger.LogWarning("Subteam not found with Id: {SubTeamId}", id);
                    return NotFound("Subteam not found.");
                }
                _logger.LogInformation("Subteam retrieved successfully with Id: {SubTeamId}", id);
                return Ok(subTeam);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subteam with Id: {SubTeamId}", id);
                return StatusCode(500, "An error occurred while retrieving the subteam.");
            }
        }

        /// <summary>
        /// Get all subteams
        /// </summary>
        /// <returns>List of all subteams</returns>
        [HttpGet("get-all-subteams")]
        public async Task<ActionResult<IEnumerable<SubTeamDto>>> GetAllSubTeams()
        {
            _logger.LogInformation("Retrieving all subteams");
            try
            {
                var subTeams = await _subTeamService.GetAllSubTeamsAsync();
                _logger.LogInformation("Retrieved {Count} subteams successfully", subTeams?.Count() ?? 0);
                return Ok(subTeams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all subteams");
                return StatusCode(500, "An error occurred while retrieving subteams.");
            }
        }

        /// <summary>
        /// Update subteam information
        /// </summary>
        /// <param name="id">Subteam identifier</param>
        /// <param name="subTeamDto">Updated subteam data</param>
        /// <returns>Updated subteam</returns>
        [HttpPut("update-subteam/{id}")]
        [Authorize(Policy ="Admin")]
        public async Task<ActionResult<SubTeamDto>> UpdateSubTeam(Guid id, [FromBody] SubTeamDto subTeamDto)
        {
            _logger.LogInformation("Updating subteam with Id: {SubTeamId}", id);
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { Errors = errors });
                }

                if (subTeamDto == null)
                {
                    _logger.LogWarning("UpdateSubTeam failed: SubTeamDto is null for Id: {SubTeamId}", id);
                    return BadRequest("SubTeam data is required.");
                }

                var updatedSubTeam = await _subTeamService.UpdateSubTeamAsync(id, subTeamDto);
                if (updatedSubTeam == null)
                {
                    _logger.LogWarning("Subteam not found with Id: {SubTeamId}", id);
                    return NotFound("Subteam not found.");
                }
                _logger.LogInformation("Subteam updated successfully with Id: {SubTeamId}", id);
                return Ok(updatedSubTeam);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Error updating subteam with Id: {SubTeamId}: {ErrorMessage}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subteam with Id: {SubTeamId}", id);
                return StatusCode(500, "An error occurred while updating the subteam.");
            }
        }

        /// <summary>
        /// Delete a subteam
        /// </summary>
        /// <param name="id">Subteam identifier</param>
        /// <returns>Delete result</returns>
        [HttpDelete("delete-subteam/{id}")]
        public async Task<ActionResult> DeleteSubTeam(Guid id)
        {
            _logger.LogInformation("Deleting subteam with Id: {SubTeamId}", id);
            try
            {
                var deleted = await _subTeamService.DeleteSubTeamAsync(id);
                if (!deleted)
                {
                    _logger.LogWarning("Subteam not found with Id: {SubTeamId}", id);
                    return NotFound("Subteam not found.");
                }
                _logger.LogInformation("Subteam deleted successfully with Id: {SubTeamId}", id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error deleting subteam with Id: {SubTeamId}: {ErrorMessage}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subteam with Id: {SubTeamId}", id);
                return StatusCode(500, "An error occurred while deleting the subteam.");
            }
        }
        /// <summary>
        /// Get subteams by team name
        /// </summary>
        /// <param name="teamName">Team name</param>
        /// <returns>List of subteams for the team</returns>
        [HttpGet("get-subteams-by-team/{teamId}")]
        public async Task<ActionResult<IEnumerable<SubTeamDto>>> GetSubTeamsByTeamId(string teamName)
        {
            _logger.LogInformation("Retrieving subteams for team: {TeamName}", teamName);
            try
            {
                if (string.IsNullOrWhiteSpace(teamName))
                {
                    _logger.LogWarning("GetSubTeamsByTeamName failed: TeamName is empty or null");
                    return BadRequest("Team name is required.");
                }

                var subTeams = await _subTeamService.GetSubTeamsByTeamNameAsync(teamName);
                if (subTeams == null )
                {
                    _logger.LogWarning("No subteams found for team: {TeamName}", teamName);
                    return NotFound("No subteams found for the specified team.");
                }
                _logger.LogInformation("Retrieved {Count} subteams for team: {TeamName}", subTeams, teamName);
                return Ok(subTeams);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Error retrieving subteams for team: {TeamName}, Error: {ErrorMessage}", teamName, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subteams for team: {TeamName}", teamName);
                return StatusCode(500, "An error occurred while retrieving subteams.");
            }
        }
    }
}



