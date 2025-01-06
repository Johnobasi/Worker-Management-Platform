using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IDevotionalRepository
    {
        Task AddDevotionalAsync(Devotional devotional);
        Task<Devotional> GetDevotionalByIdAsync(Guid id);
        Task<IEnumerable<Devotional>> GetAllDevotionalsAsync();
        Task DeleteDevotionalAsync(Guid id);
    }
}
