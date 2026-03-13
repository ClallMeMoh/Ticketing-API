using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Repositories;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Repositories;

public class AgentProfileRepository : IAgentProfileRepository
{
    private readonly ApplicationDbContext _context;

    public AgentProfileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AgentProfile?> GetByUserIdAsync(Guid userId)
        => await _context.AgentProfiles.FirstOrDefaultAsync(a => a.UserId == userId);

    public async Task<List<AgentProfile>> GetAllAsync()
        => await _context.AgentProfiles.Include(a => a.User).ToListAsync();

    public async Task AddAsync(AgentProfile profile)
        => await _context.AgentProfiles.AddAsync(profile);

    public void Update(AgentProfile profile)
        => _context.AgentProfiles.Update(profile);
}
