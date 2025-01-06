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

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IQRCodeRepository, QRCodeRepository>();
            services.AddScoped<IUserRegistrationTokenRepository, UserRegistrationTokenRepository>();
            services.AddScoped<IEmailService, SmtpEmailService>();
            services.AddScoped<IAttendanceRepository, AttendanceRepository>();
            services.AddScoped<IWorkerRewardRepository, WorkerRewardRepository>();
            services.AddScoped<IDevotionalRepository, DevotionalRepository>();

            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<ITeamRepository, TeamRepository>();


            services.AddHostedService<SundayRewardProcessor>();

            services.Configure<EmailConfiguration>(configuration.GetSection("Email"));
            return services;
        }
    }
}
