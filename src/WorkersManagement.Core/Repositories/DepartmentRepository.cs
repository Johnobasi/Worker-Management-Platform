using Microsoft.EntityFrameworkCore;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Core.Repositories
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly WorkerDbContext _context;
        public DepartmentRepository(WorkerDbContext workerDbContext)
        {
            _context = workerDbContext;
        }
        public async Task<Department> CreateDepartmentAsync(Department department)
        {
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return department;
        }

        public async Task<Department?> GetDepartmentByIdAsync(Guid departmentId)
        {
            return await _context.Departments.Include(d => d.Users).FirstOrDefaultAsync(d => d.Id == departmentId);
        }

        public async Task<IEnumerable<Department>> GetDepartmentsByTeamIdAsync(Guid teamId)
        {
            return await _context.Departments.Where(d => d.TeamId == teamId).ToListAsync();
        }
    }
}
