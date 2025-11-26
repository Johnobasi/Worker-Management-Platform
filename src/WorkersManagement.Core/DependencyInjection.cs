using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
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

            services.AddScoped<IWorkersAuthRepository, WorkerAuthRepository>();
            services.AddScoped<IHabitCompletionRepository, HabitCompletionRepository>();
            services.AddScoped<ISubTeamRepository, SubTeamRepository>();

            services.AddScoped<ITemplateDesignerService, TemplateDesignerService>();
            services.AddScoped<IJwt, JWTService>();

            services.AddScoped<IHabitPreference, HabitPreferenceRepository>();

            services.Configure<EmailSettings>(configuration.GetSection(nameof(EmailSettings)));
            services.AddQuartzHostedService(q =>
            {
                q.WaitForJobsToComplete = true;
            });

            services.AddQuartz(configure =>
            {
                var jobKey = new JobKey(nameof(SundayRewardProcessor));

                configure.AddJob<SundayRewardProcessor>(opts => opts.WithIdentity(jobKey));
                configure.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("SundayRewardTrigger")
                    .WithCronSchedule("0 0 10 ? * SAT *"));

                configure.UseMicrosoftDependencyInjectionJobFactory();
            });

            return services;
        }
    }
}
