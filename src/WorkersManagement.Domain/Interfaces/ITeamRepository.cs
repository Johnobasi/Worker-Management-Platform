using WorkersManagement.Domain.Dtos;
using WorkersManagement.Infrastructure;

namespace WorkersManagement.Domain.Interfaces
{
    public interface ITeamRepository 
    {
        Task<Team> CreateTeamAsync(Team team);
        Task<List<Team>> GetAllTeamsAsync();
        Task<Team> GetTeamByNameAsync(string teamName);

        Task<bool> UpdateTeamAsync(UpdateTeamDto team);
        Task<bool> DeleteTeamAsync(Guid teamId);
    }

}
