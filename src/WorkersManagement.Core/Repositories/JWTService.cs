using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;

namespace WorkersManagement.Core.Repositories
{
    public class JWTService : IJwt
    {
        private readonly IConfiguration _configuration;
        public JWTService(IConfiguration configuration)
        {
           _configuration = configuration;
        }
        public string GenerateJwtToken(Worker worker)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);
            var claims = new List<Claim>
            {
                new("firstName", worker.FirstName),
                new("lastName", worker.LastName),
                new("workerId", worker.Id.ToString()),
                new(ClaimTypes.NameIdentifier, worker.WorkerNumber),
                new(ClaimTypes.Email, worker.Email)
            };

            // Add a Claim for each role
            foreach (var role in worker.Roles.Select(r =>
                             r is Enum ? r.ToString() :
                             r.GetType().GetProperty("Role")?.GetValue(r)?.ToString()))
            {
                if (!string.IsNullOrWhiteSpace(role))
                    claims.Add(new Claim(ClaimTypes.Role, role!));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
