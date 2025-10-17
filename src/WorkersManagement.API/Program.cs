using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using WorkersManagement.Core;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Enumerations;
namespace WorkersManagement.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            builder.Services.AddControllers()
             .AddJsonOptions(options =>
             {
                 options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                 options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
             });

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder!.Configuration["Jwt:Key"]))
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("SuperAdmin", policy => policy.RequireRole(UserRole.SuperAdmin.ToString()));
                options.AddPolicy("Admin", policy => policy.RequireRole(
                    UserRole.Admin.ToString(),
                    UserRole.SubTeamLead.ToString(),
                    UserRole.SuperAdmin.ToString()));
                options.AddPolicy("SubTeamLead", policy => policy.RequireRole(
                    UserRole.SubTeamLead.ToString(),
                    UserRole.Admin.ToString(),
                    UserRole.SuperAdmin.ToString()));
                options.AddPolicy("HOD", policy => policy.RequireRole(
                    UserRole.HOD.ToString(),
                    UserRole.SubTeamLead.ToString(),
                    UserRole.Admin.ToString(),
                    UserRole.SuperAdmin.ToString()));
                options.AddPolicy("Worker", policy => policy.RequireRole(
                    UserRole.Worker.ToString(),
                    UserRole.HOD.ToString(),
                    UserRole.SubTeamLead.ToString(),
                    UserRole.Admin.ToString(),
                    UserRole.SuperAdmin.ToString()));

                // Department-specific policy for HODs
                options.AddPolicy("DepartmentHOD", policy =>
                    policy.RequireRole(UserRole.HOD.ToString())
                          .RequireClaim("DepartmentId"));


            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("CanCreateWorkers", policy =>
                    policy.RequireRole("Admin", "SuperAdmin", "SubTeamLead"));
            });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
           
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' followed by a space and your token."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Enable XML comments for better documentation
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);

                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Workers Attendance API",
                    Version = "v1",
                    Description = "CRM API for managing workers.",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "Harvesters UK",
                        Email = "support@example.com",
                        Url = new Uri("https://yourcompany.com")
                    },
                    License = new Microsoft.OpenApi.Models.OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });

            });

            //add infrastructure and other services
            builder.Services.AddInfrastructure(builder.Configuration);

            builder.Services.AddCore(builder.Configuration);
            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();
                if (dbContext.Database.IsRelational())
                {
                    dbContext.Database.Migrate();
                }
            }
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseCors();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
