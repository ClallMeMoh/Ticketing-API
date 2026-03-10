using Ticketing.Domain.Entities;

namespace Ticketing.Domain.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id);
    Task<AppUser?> GetByEmailAsync(string email);
    Task AddAsync(AppUser user);
    void Update(AppUser user);
    Task<bool> ExistsAsync(string email);
    Task<List<AppUser>> GetAllAsync();
}
