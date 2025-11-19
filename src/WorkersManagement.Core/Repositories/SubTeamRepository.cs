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

            _dbContext.SubTeams.Add(subTeamDto);
            await _dbContext.SaveChangesAsync();

            return new CreateSubTeamDto
            {
                Name = subTeamDto.Name,
                TeamName = subTeamDto.Team.Name,
                Description = subTeamDto.Description
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
                    Id = st.Id,
                    Name = st.Name,
                    Description = st.Description,
                    TeamName = st.Team.Name
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

            _dbContext.SubTeams.Remove(subTeam);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<SubTeam> GetSubTeamsByTeamNameAsync(string teamName)
        {
            return await _dbContext.SubTeams
             .Include(st => st.Team)
             .FirstOrDefaultAsync(st => st.Name == teamName);
        }
    }
}
