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

        Task<Worker> CreateWorkerAsync(CreateNewWorkerDto dto);
        Task<Worker> GetWorkerByNumberAsync(string workerNumber);
    }
}
