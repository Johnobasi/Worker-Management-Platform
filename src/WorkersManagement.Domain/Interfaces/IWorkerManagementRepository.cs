using WorkersManagement.Domain.Dtos;
using WorkersManagement.Infrastructure;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IWorkerManagementRepository
    {
        Task<Worker> GetWorkerByIdAsync(Guid id);
        Task<ICollection<Worker>> GetAllWorkersAsync();
        Task UpdateWorkerAsync(Worker user);
        Task DeleteWorkerAsync(Guid id);

        Task<CreateWorkerResult> CreateWorkerAsync(CreateNewWorkerDto dto);
        Task<Worker> GetWorkerByNumberAsync(string workerNumber);
        Task<List<Worker>> GetAllWorkersForEmailAsync();
        Task<List<Worker>> GetWorkersByIdsAsync(List<Guid> workerIds);
    }
}
