using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Repositories;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppUser?> GetByIdAsync(Guid id)
        => await _context.Users.FindAsync(id);

    public async Task<AppUser?> GetByEmailAsync(string email)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task AddAsync(AppUser user)
        => await _context.Users.AddAsync(user);

    public async Task<bool> ExistsAsync(string email)
        => await _context.Users.AnyAsync(u => u.Email == email);

    public void Update(AppUser user)
        => _context.Users.Update(user);

    public async Task<List<AppUser>> GetAllAsync()
        => await _context.Users.ToListAsync();
}
