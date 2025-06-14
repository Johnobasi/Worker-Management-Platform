﻿using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Domain.Dtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Core.Repositories
{
    public class WokerManagementRepository(WorkerDbContext workerDbContext,
        ILogger<WokerManagementRepository> logger, IDepartmentRepository departmentRepository, IEmailService emailService) : IWorkerManagementRepository
    {
        private readonly WorkerDbContext _context = workerDbContext;
        private readonly ILogger<WokerManagementRepository> _logger = logger;
        private readonly IDepartmentRepository _departmentRepository = departmentRepository;
       private readonly IEmailService _emailService = emailService;
        private readonly string _profilePictureStoragePath = "uploads/ProfilePictures";

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

        public async Task<Worker> CreateWorkerAsync(CreateNewWorkerDto dto)
        {
            _logger.LogInformation("Creating new user with email: {Email}", dto.Email);

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ArgumentException("Email is required.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Password is required.");

            Department? department = null;


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

            if (!TeamCodeMap.TryGetValue(team.Name, out string? teamCode))
            {
                _logger.LogError("Team code not found for team name: '{TeamName}'. Available team names: {AvailableTeams}",
                    team.Name, string.Join(", ", TeamCodeMap.Keys));
                throw new ArgumentException($"Team code for '{team.Name}' not defined.");
            }
            

            try
            {
                var nextWorkerId = await _context.Workers.CountAsync() + 1;
                var numericPart = nextWorkerId.ToString("D3");
                var workerNumber = $"{teamCode}{numericPart}";

                // Hash password
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password.Trim());
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
                    Role = dto.Role,
                    Department = department,
                    DepartmentId = department?.Id, // Set foreign key
                    WorkerNumber = workerNumber,
                    PasswordHash = passwordHash,
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

                // Generate reset link
                var resetLink = $"https://www.harvestersuk.org/verify-token?email={Uri.EscapeDataString(workerToAdd.Email)}&token={resetToken}";

                // Load and populate the HTML template
                var subject = "Set Your Password - Workers CMS";
                string body = await LoadAndPopulateEmailTemplate(workerToAdd, resetLink);

                // Send password reset email
                await _emailService.SendEmailAsync(workerToAdd.Email, subject,body);

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
    }
}
