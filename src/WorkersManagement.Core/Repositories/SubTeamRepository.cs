using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Dtos.SubTeam;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Core.Repositories
{
    internal class SubTeamRepository : ISubTeamRepository
    {
        private readonly WorkerDbContext _dbContext;

        public SubTeamRepository(WorkerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CreateSubTeamDto> CreateSubTeamAsync(SubTeam subTeamDto)
        {
            if (subTeamDto == null)
                throw new ArgumentNullException(nameof(subTeamDto));

            if (!await _dbContext.Teams.AnyAsync(t => t.Name == subTeamDto.Name))
                throw new ArgumentException("Invalid TeamId. Team does not exist.");

            var subTeam = new SubTeam
            {

                Name = subTeamDto.Name,
                Description = subTeamDto.Description
            };

            _dbContext.SubTeams.Add(subTeam);
            await _dbContext.SaveChangesAsync();

            return new CreateSubTeamDto
            {
                Name = subTeamDto.Name,
                TeamName = subTeamDto.Team.Name
            };
        }

        public async Task<SubTeamDto> GetSubTeamByIdAsync(Guid id)
        {
            var subTeam = await _dbContext.SubTeams
                .AsNoTracking()
                 .Include(x => x.Team)
                .ThenInclude(c => c.Departments)
                .FirstOrDefaultAsync(st => st.Id == id);

            if (subTeam == null)
                return null!;

            return new SubTeamDto
            {
                Name = subTeam.Name,
                Description = subTeam.Description
            };
        }

        public async Task<IEnumerable<SubTeamDto>> GetAllSubTeamsAsync()
        {
            return await _dbContext.SubTeams
                .AsNoTracking()
                .Include(x=>x.Team)
                .ThenInclude(c=>c.Departments)
                .Select(st => new SubTeamDto
                {
                    Name = st.Name,
                    Description = st.Description
                })
                .ToListAsync();
        }

        public async Task<SubTeamDto> UpdateSubTeamAsync(Guid id, SubTeamDto subTeamDto)
        {
            if (subTeamDto == null)
                throw new ArgumentNullException(nameof(subTeamDto));

            var subTeam = await _dbContext.SubTeams
                .FirstOrDefaultAsync(st => st.Id == id);

            if (subTeam == null)
                return null!;

            subTeam.Name = subTeamDto.Name;
            subTeam.Description = subTeamDto.Description;

            await _dbContext.SaveChangesAsync();

            return subTeamDto;
        }

        public async Task<bool> DeleteSubTeamAsync(Guid id)
        {
            var subTeam = await _dbContext.SubTeams
                .FirstOrDefaultAsync(st => st.Id == id);

            if (subTeam == null)
                return false;

            //if (await _dbContext.Departments.AnyAsync(d => d.Subteam.Id == id) ||
            //    await _dbContext.Workers.AnyAsync(w => w.SubTeamId == id))
            //    throw new InvalidOperationException("Cannot delete SubTeam with associated Departments or Workers.");

            _dbContext.SubTeams.Remove(subTeam);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<SubTeamDto>> GetSubTeamsByTeamNameAsync(string teamName)
        {
            if (!await _dbContext.Teams.AnyAsync(t => t.Name == teamName))
                throw new ArgumentException("Invalid Team name. Team does not exist.");

            return await _dbContext.SubTeams
                .AsNoTracking()
                .Where(st => st.Name == teamName)
                .Select(st => new SubTeamDto
                {
                    Name = st.Name,
                    Description = st.Description
                })
                .ToListAsync();
        }
    }
}
