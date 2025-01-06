using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<Department> CreateDepartmentAsync(Department department);
        Task<IEnumerable<Department>> GetDepartmentsByTeamIdAsync(Guid teamId);
        Task<Department> GetDepartmentByIdAsync(Guid departmentId);
    }
}
