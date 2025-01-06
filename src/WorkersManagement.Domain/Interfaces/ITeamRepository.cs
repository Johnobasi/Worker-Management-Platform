using WorkersManagement.Infrastructure;

namespace WorkersManagement.Domain.Interfaces
{
    public interface ITeamRepository 
    {
        Task<Team> CreateTeamAsync(Team team);
        Task<List<Team>> GetAllTeamsAsync();
        Task<Team> GetTeamByIdAsync(Guid teamId);
    }

}
