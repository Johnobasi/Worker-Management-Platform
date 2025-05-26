using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkersManagement.Core.Abstract;
using WorkersManagement.Core.BackGroundJob;
using WorkersManagement.Core.Repositories;
using WorkersManagement.Domain.EmailConfigs;
using WorkersManagement.Domain.Interfaces;

namespace WorkersManagement.Core
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddScoped<IWorkerManagementRepository, WokerManagementRepository>();
            services.AddScoped<IBarcodeRepository, QRCodeRepository>();
            services.AddScoped<IEmailService, SmtpEmailService>();
            services.AddScoped<IAttendanceRepository, AttendanceRepository>();
            services.AddScoped<IWorkerRewardRepository, WorkerRewardRepository>();
            services.AddScoped<IDevotionalRepository, DevotionalRepository>();

            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<ITeamRepository, TeamRepository>();
            services.AddScoped<IHabitRepository, HabitRepository>();


            services.AddHostedService<SundayRewardProcessor>();

            services.Configure<EmailConfiguration>(configuration.GetSection("Email"));
            return services;
        }
    }
}
