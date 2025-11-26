using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Core.Repositories
{
    public class WokerManagementRepository(WorkerDbContext workerDbContext,
        ILogger<WokerManagementRepository> logger, IDepartmentRepository departmentRepository, 
        IWorkersAuthRepository authRepository,
        IJwt jwt) : IWorkerManagementRepository
    {
        private readonly WorkerDbContext _context = workerDbContext;
        private readonly ILogger<WokerManagementRepository> _logger = logger;
        private readonly IDepartmentRepository _departmentRepository = departmentRepository;
        private readonly string _profilePictureStoragePath = "uploads/ProfilePictures";
        private readonly IWorkersAuthRepository _authRepository = authRepository;
        private readonly IJwt _jwtService = jwt;

        private static readonly Dictionary<string, string> TeamCodeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Maturity", "MAR-" },
            { "Ministry", "MIN-" },
            { "Membership","MEM-" },
            { "Program", "PROG-" },
            { "Kidzone", "KID-" },
            { "General Service","GEN-" },
            { "Missions", "MISS-" }
        };

        public async Task<CreateWorkerResult> CreateWorkerAsync(CreateNewWorkerDto dto)
        {
            _logger.LogInformation("Creating new user with email: {Email}", dto.Email);

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ArgumentException("Email is required.");

            Department? department = null;


            if (string.IsNullOrWhiteSpace(dto.DepartmentName))
                throw new ArgumentException("Department name is required for a Worker role.");

            department = await _departmentRepository.GetDepartmentByNameAsync(dto.DepartmentName.Trim());

            if (department == null)
                throw new ArgumentException("Department {DepartmentName}  does not exist.", dto.DepartmentName);

            if (department.TeamId == Guid.Empty)
                throw new ArgumentException("Department {DepartmentName} is not linked to any team.", dto.DepartmentName);

            var team = await _context.Teams.FindAsync(department.TeamId);
            if (team == null)
                throw new ArgumentException("Team not found for department.");

            if (!TeamCodeMap.TryGetValue(team.Name, out string? teamCode))
            {
                _logger.LogError("Team code not found for team name: '{TeamName}'. Available team names: {AvailableTeams}",
                    team.Name, string.Join(", ", TeamCodeMap.Keys));
                throw new ArgumentException("Team code for {TeamName} not defined.", team.Name);
            }
            

            try
            {
                var nextWorkerId = await _context.Workers.CountAsync() + 1;
                var numericPart = nextWorkerId.ToString("D3");
                var workerNumber = $"{teamCode}{numericPart}";

                // Generate password reset token
                var resetToken = Guid.NewGuid().ToString();
                var resetTokenExpiration = DateTime.UtcNow.AddHours(24); // Token expires in 24 hours

                // Handle profile picture upload
                string? profilePictureUrl = null;
                if (dto.ProfilePicture != null)
                {
                    profilePictureUrl = await SaveProfilePictureAsync(dto.ProfilePicture, workerNumber);
                }

                var workerToAdd = new Worker
                {
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Type = dto.WorkerType.ToList(),
                    Department = department,
                    DepartmentId = department?.Id, // Set foreign key
                    WorkerNumber = workerNumber,
                    LastLogin = null,
                    PasswordResetToken = resetToken,
                    PasswordResetTokenExpiration = resetTokenExpiration,
                    Status = true,
                    ProfilePictureUrl = profilePictureUrl,
                    Attendances = [],
                    Habits = [],
                    HabitCompletions = [],
                    Rewards = []
                };

                await _context.Workers.AddAsync(workerToAdd);
                await _context.SaveChangesAsync();

                if (dto.HabitPreferences?.Any() == true)
                {
                    var preferences = dto.HabitPreferences.Select(habit => new WorkerHabitPreference
                    {
                        WorkerId = workerToAdd.Id,
                        HabitType = habit
                    }).ToList();

                    await _context.WorkerHabitPreferences.AddRangeAsync(preferences);
                    await _context.SaveChangesAsync();
                }

                await _authRepository.SendPasswordSetupEmailAsync(workerToAdd);

                var jwtToken = _jwtService.GenerateJwtToken(workerToAdd);
                _logger.LogInformation("Worker created with WorkerNumber: {WorkerNumber}", workerNumber);

               return new CreateWorkerResult
               {
                    WorkerId = workerToAdd.Id,
                    WorkerNumber = workerToAdd.WorkerNumber,
                    Email = workerToAdd.Email,
                    FirstName = workerToAdd.FirstName,
                    LastName = workerToAdd.LastName,
                    JwtToken = jwtToken,
                    CreatedAt = DateTime.UtcNow
               };
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
                var worker = await GetWorkerByIdAsync(id) ?? throw new InvalidOperationException("Worker not found.");
                _context.Workers.Remove(worker);
                await _context.SaveChangesAsync();              
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
                return await _context.Workers
                    .Include(d=>d.Department)
                    .Include(w => w.HabitPreferences)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<Worker>();
            }

        }

        public async Task<List<Worker>> SearchWorkersAsync(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return new List<Worker>();

                query = query.Trim().ToLower();

                return await _context.Workers
                    .Include(w => w.Department)
                    .Include(w => w.Habits)
                        .ThenInclude(h => h.Completions)
                    .Where(w =>
                        w.FirstName.ToLower().Contains(query) ||
                        w.LastName.ToLower().Contains(query) ||
                        w.Email.ToLower().Contains(query) ||
                        w.WorkerNumber.ToLower().Contains(query)
                    )
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for workers");
                return new List<Worker>();
            }
        }
        public async Task<Worker?> GetWorkerByIdAsync(Guid id)
        {
            try
            {
                return await _context.Workers
                    .Include(d => d.Department)
                        .Include(w => w.Habits)
                            .ThenInclude(d => d.Completions)
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

        private async Task<string> LoadAndPopulateEmailTemplate(Worker worker, string resetLink)
        {
            try
            {

                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string templatePath = Path.Combine(baseDirectory, "Templates", "PasswordResetNewWorkerTemplate.html");
                if (!File.Exists(templatePath))
                {
                    _logger.LogError("Email template file not found at {Path}", templatePath);
                    throw new FileNotFoundException("Email template file not found", templatePath);
                }

                string template = await File.ReadAllTextAsync(templatePath);

                template = template
                    .Replace("{FirstName}", worker.FirstName)
                    .Replace("{LastName}", worker.LastName)
                    .Replace("{ResetLink}", resetLink);

                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load or populate email template for {Email}", worker.Email);
                throw;
            }
        }

        private async Task<string> SaveProfilePictureAsync(IFormFile file, string workerNumber)
        {
            // Validate file
            if (file.Length == 0)
                throw new ArgumentException("Profile picture file is empty.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException("Invalid file type. Only JPG, JPEG, and PNG are allowed.");

            // Generate unique file name
            var fileName = $"{workerNumber}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_profilePictureStoragePath, fileName);

            // Ensure directory exists
            Directory.CreateDirectory(_profilePictureStoragePath);

            // Save file to local storage (or cloud storage in production)
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return URL (adjust based on your hosting setup)
            return $"/{_profilePictureStoragePath}/{fileName}";
        }

        public async Task<List<Worker>> GetAllWorkersForEmailAsync()
        {
            try
            {
                return await _context.Workers
                    .Where(w => !string.IsNullOrEmpty(w.Email))
                    .Include(w => w.Department)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get workers for email");
                return new List<Worker>();
            }
        }

        public async Task<List<Worker>> GetWorkersByIdsAsync(List<Guid> workerIds)
        {
            try
            {
                return await _context.Workers
                    .Where(w => workerIds.Contains(w.Id) && !string.IsNullOrEmpty(w.Email))
                    .Include(w => w.Department)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get workers by IDs");
                return new List<Worker>();
            }
        }


    }
}
