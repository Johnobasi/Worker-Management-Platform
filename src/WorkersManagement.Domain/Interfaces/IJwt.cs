using WorkersManagement.Infrastructure;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IJwt
    {
        public string GenerateJwtToken(Worker worker);
    }
}
