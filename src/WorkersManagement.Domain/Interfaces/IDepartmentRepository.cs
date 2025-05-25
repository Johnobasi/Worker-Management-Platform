using WorkersManagement.Domain.Dtos;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<List<Department>> AllDepartmentsAsync();
        Task<DepartmentDto> CreateDepartmentAsync(Department department);
        Task<IEnumerable<Department>> GetDepartmentsByTeamIdAsync(Guid teamId);
        Task<Department> GetDepartmentByNameAsync(string departmentName);
        Task<bool> UpdateDepartmentAsync(UpdateDepartmentDto department);
        Task<bool> DeleteDepartmentAsync(Guid departmentId);

    }
}
