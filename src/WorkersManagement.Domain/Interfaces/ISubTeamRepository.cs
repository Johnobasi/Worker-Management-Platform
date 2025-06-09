using WorkersManagement.Domain.Dtos.SubTeam;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Domain.Interfaces
{
    public interface ISubTeamRepository
    {
        Task<CreateSubTeamDto> CreateSubTeamAsync(SubTeam subTeamDto);
        Task<SubTeamDto> GetSubTeamByIdAsync(Guid id);
        Task<IEnumerable<SubTeamDto>> GetAllSubTeamsAsync();
        Task<SubTeamDto> UpdateSubTeamAsync(Guid id, SubTeamDto subTeamDto);
        Task<bool> DeleteSubTeamAsync(Guid id);
        Task<SubTeam> GetSubTeamsByTeamNameAsync(string teamName);
    }

}
