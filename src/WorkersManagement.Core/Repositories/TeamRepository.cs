using Microsoft.EntityFrameworkCore;
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

        public async Task<Team?> GetTeamByIdAsync(Guid teamId)
        {
           return await _context.Teams
                .Include(t => t.Departments)
                .FirstOrDefaultAsync(t => t.Id == teamId);
        }
    }
}
