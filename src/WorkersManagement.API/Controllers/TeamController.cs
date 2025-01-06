using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamController : ControllerBase
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly ITeamRepository _teamRepository;
        public TeamController(IDepartmentRepository departmentRepository, ITeamRepository teamRepository)
        {
            _departmentRepository = departmentRepository;
            _teamRepository = teamRepository;
        }

        [HttpPost("create-team")]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto request)
        {
            if (string.IsNullOrEmpty(request.Name))
                return BadRequest("Team name is required.");

            var team = new Team
            {
                Name = request.Name,
                Description = request.Description
            };

            var createdTeam = await _teamRepository.CreateTeamAsync(team);
            return Ok(createdTeam);
        }

        [HttpGet("get-teams")]
        public async Task<IActionResult> GetTeams()
        {
            var teams = await _teamRepository.GetAllTeamsAsync();
            return Ok(teams);
        }

        [HttpPost("add-department")]
        public async Task<IActionResult> AddDepartment([FromBody] CreateDepartmenDto req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest("Department name is required.");

            var availableTeams = await _teamRepository.GetAllTeamsAsync();

            var randomTeam = availableTeams.OrderBy(r => Guid.NewGuid()).FirstOrDefault();

            if (randomTeam == null)
                return NotFound("No teams available in the system.");

            var department = new Department
            {
                Id = Guid.NewGuid(),
                Name = req.Name.Trim(),
                TeamId = randomTeam.Id
            };

            var createdDepartment = await _departmentRepository.CreateDepartmentAsync(department);
            return Ok(createdDepartment);
        }
    }
}
