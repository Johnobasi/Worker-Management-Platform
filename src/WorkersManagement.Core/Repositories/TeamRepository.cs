using Microsoft.EntityFrameworkCore;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;

namespace WorkersManagement.Core.Repositories
{
    public class TeamRepository(WorkerDbContext workerDbContext) : ITeamRepository
    {
        private readonly WorkerDbContext _context = workerDbContext;

        public async Task<Team> CreateTeamAsync(Team team)
        {
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
            return team;
        }

        public async Task<List<Team>> GetAllTeamsAsync()
        {
            return await _context.Teams
                .Include(t=>t.Departments)
                .ToListAsync();
        }

        public async Task<Team?> GetTeamByNameAsync(string teamName)
        {
            return await _context.Teams
                .Include(t => t.Departments)
                .FirstOrDefaultAsync(t => t.Name.Trim().ToLower() == teamName.Trim().ToLower());
        }

        public async Task<bool> UpdateTeamAsync(UpdateTeamDto team)
        {
            var existingTeam = await _context.Teams.FindAsync(team.Id);
            if (existingTeam == null) return false;

            existingTeam.Name = team.Name;
            existingTeam.Description = team.Description;

            _context.Teams.Update(existingTeam);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTeamAsync(Guid teamId)
        {
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return false;

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
