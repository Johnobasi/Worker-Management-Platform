using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IUserRegistrationTokenRepository
    {
        Task SaveTokenAsync(UserRegistrationToken token);
        Task<UserRegistrationToken> GetTokenAsync(string token);
        Task MarkTokenAsUsedAsync(UserRegistrationToken token);
    }
}
