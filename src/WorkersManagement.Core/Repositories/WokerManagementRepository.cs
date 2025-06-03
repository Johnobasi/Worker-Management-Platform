using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Core.Repositories
{
    public class WokerManagementRepository(WorkerDbContext workerDbContext,
        ILogger<WokerManagementRepository> logger, IDepartmentRepository departmentRepository) : IWorkerManagementRepository
    {
        private readonly WorkerDbContext _context = workerDbContext;
        private readonly ILogger<WokerManagementRepository> _logger = logger;
        private readonly IDepartmentRepository _departmentRepository = departmentRepository;

        private static readonly Dictionary<string, string> TeamCodeMap = new()
        {
            { "Maturity", "MAR-" },
            { "Ministry", "MIN-" },
            { "Membership", "MEM-" },
            { "Program", "PROG-" },
            { "Kidzone", "KID-" },
            { "General Service", "GEN-" },
            { "Missions", "MISS-" }
        };

        public async Task<Worker> CreateWorkerAsync(CreateNewWorkerDto dto)
        {
            _logger.LogInformation("Creating new user with email: {Email}", dto.Email);

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ArgumentException("Email is required.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Password is required.");

            Department? department = null;
            string? teamCode = null;

            if (dto.Role == UserRole.Worker)
            {
                if (string.IsNullOrWhiteSpace(dto.DepartmentName))
                    throw new ArgumentException("Department name is required for a Worker role.");

                department = await _departmentRepository.GetDepartmentByNameAsync(dto.DepartmentName.Trim());

                if (department == null)
                    throw new ArgumentException($"Department '{dto.DepartmentName}' does not exist.");

                if (department.TeamId == Guid.Empty)
                    throw new ArgumentException($"Department '{dto.DepartmentName}' is not linked to any team.");

                var team = await _context.Teams.FindAsync(department.TeamId);
                if (team == null)
                    throw new ArgumentException("Team not found for department.");

                if (!TeamCodeMap.TryGetValue(team.Name.Trim(), out teamCode))
                    throw new ArgumentException($"Team code for '{team.Name}' not defined.");
            }

            try
            {
                var nextWorkerId = await _context.Workers.CountAsync() + 1;
                var numericPart = nextWorkerId.ToString("D3");
                var workerNumber = $"{teamCode}{numericPart}";

                // Hash password
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password.Trim());


                var workerToAdd = new Worker
                {
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Role = UserRole.Worker,
                    Department = department,
                    WorkerNumber = workerNumber,
                    PasswordHash = passwordHash,
                    LastLogin = null,
                    PasswordResetToken = null,
                    PasswordResetTokenExpiration = null
                };

                await _context.Workers.AddAsync(workerToAdd);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Worker created with WorkerNumber: {WorkerNumber}", workerNumber);

                return workerToAdd;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user with email {Email}", dto.Email);
                throw;
            }
        }

        public async Task DeleteWorkerAsync(Guid id)
        {
            try
            {
                var user = await GetWorkerByIdAsync(id);
                if (user != null)
                {
                    _context.Workers.Remove(user);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        public async Task<ICollection<Worker>> GetAllWorkersAsync()
        {
            try
            {
                return await _context.Workers.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<Worker>();
            }

        }

        public async Task<Worker?> GetWorkerByIdAsync(Guid id)
        {
            try
            {
                return await _context.Workers
                        .Include(w => w.Department)
                            .ThenInclude(d => d.Teams)
                                 .FirstOrDefaultAsync(w => w.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }

        public async Task UpdateWorkerAsync(Worker worker)
        {
            try
            {
                _context.Set<Worker>().Update(worker);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        public async Task<Worker?> GetWorkerByNumberAsync(string workerNumber)
        {
            return await _context.Workers.FirstOrDefaultAsync(w => w.WorkerNumber == workerNumber);
        }
    }
}
