using Microsoft.EntityFrameworkCore;
using WorkersManagement.Domain.Dtos;
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
        public async Task<DepartmentDto> CreateDepartmentAsync(Department department)
        {
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            //return new DepartmentDto
            //{
            //    Name = department.Name,
            //    TeamName = department.Teams.Name
            //};

            string containerName = department.Subteams != null
            ? department.Subteams.Name
            : department.Teams.Name;

            return new DepartmentDto
            {
                Name = department.Name,
                TeamName = containerName
            };
        }

        public async Task<Department?> GetDepartmentByNameAsync(string  departmentName)
        {
            return await _context.Departments
                .Include(d => d.Workers)
                .Include(d=>d.Teams)
                .FirstOrDefaultAsync(t => t.Name.Trim().ToLower() == departmentName.Trim().ToLower());
        }

        public async Task<IEnumerable<Department>> GetDepartmentsByTeamIdAsync(Guid teamId)
        {
            return await _context.Departments.Where(d => d.TeamId == teamId).ToListAsync();
        }

        public async Task<bool> UpdateDepartmentAsync(UpdateDepartmentDto department)
        {
            var existingDepartment = await _context.Departments.FindAsync(department.Id);
            if (existingDepartment == null) return false;

            existingDepartment.Name = department.Name;
            existingDepartment.Description = department.Description;

            _context.Departments.Update(existingDepartment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteDepartmentAsync(Guid departmentId)
        {
            var team = await _context.Departments.FindAsync(departmentId);
            if (team == null) 
                return false;

            _context.Departments.Remove(team);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Department>> AllDepartmentsAsync()
        {
            return await _context.Departments
                    .Include(d => d.Workers)
                         .Include(d => d.Teams)
                           .Include(x=>x.Subteams)
                             .ToListAsync();
        }
    }
}
