using WorkersManagement.Domain.Dtos;
using WorkersManagement.Infrastructure;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserByIdAsync(Guid id);
        Task<ICollection<User>> GetAllUsersAsync();
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(Guid id);

        Task<User> CreateUserAsync(CreateNewUserDto dto);
        Task<bool> SetPasswordAsync(SetPasswordDto dto);
    }
}
