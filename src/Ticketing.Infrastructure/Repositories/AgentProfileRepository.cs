using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
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

    public async Task<AgentProfileLoadSnapshot?> GetByUserIdWithLoadAsync(Guid userId)
    {
        var profile = await _context.AgentProfiles
            .Where(a => a.UserId == userId)
            .Select(a => new
            {
                a.UserId,
                a.User.FullName,
                a.User.Email,
                a.IsAvailable,
                a.MaxConcurrentTickets,
                a.LastAssignedAt,
                a.EfficiencyScore
            })
            .FirstOrDefaultAsync();

        if (profile is null)
            return null;

        var activeTickets = await _context.Tickets
            .Where(t =>
                t.AssignedToUserId == userId &&
                (t.Status == TicketStatus.Open ||
                 t.Status == TicketStatus.Assigned ||
                 t.Status == TicketStatus.InProgress))
            .Select(t => t.Priority)
            .ToListAsync();

        var activeCount = activeTickets.Count;
        var activeWeighted = activeTickets.Sum(GetPriorityWeight);

        return new AgentProfileLoadSnapshot(
            profile.UserId,
            profile.FullName,
            profile.Email,
            profile.IsAvailable,
            profile.MaxConcurrentTickets,
            profile.LastAssignedAt,
            profile.EfficiencyScore,
            activeCount,
            activeWeighted);
    }

    public async Task<List<AgentProfile>> GetAllAsync()
        => await _context.AgentProfiles.Include(a => a.User).ToListAsync();

    public async Task<List<AgentProfileLoadSnapshot>> GetAllWithLoadAsync()
    {
        var profiles = await _context.AgentProfiles
            .Select(a => new
            {
                a.UserId,
                a.User.FullName,
                a.User.Email,
                a.IsAvailable,
                a.MaxConcurrentTickets,
                a.LastAssignedAt,
                a.EfficiencyScore
            })
            .OrderBy(a => a.UserId)
            .ToListAsync();

        if (profiles.Count == 0)
            return [];

        var userIds = profiles.Select(p => p.UserId).ToHashSet();

        var activeTickets = await _context.Tickets
            .Where(t =>
                t.AssignedToUserId.HasValue &&
                userIds.Contains(t.AssignedToUserId.Value) &&
                (t.Status == TicketStatus.Open ||
                 t.Status == TicketStatus.Assigned ||
                 t.Status == TicketStatus.InProgress))
            .Select(t => new
            {
                UserId = t.AssignedToUserId!.Value,
                t.Priority
            })
            .ToListAsync();

        var loadByUserId = activeTickets
            .GroupBy(t => t.UserId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Count = g.Count(),
                    Weighted = g.Sum(x => GetPriorityWeight(x.Priority))
                });

        return profiles
            .Select(p =>
            {
                var hasLoad = loadByUserId.TryGetValue(p.UserId, out var load);
                var activeCount = hasLoad ? load!.Count : 0;
                var activeWeighted = hasLoad ? load!.Weighted : 0;

                return new AgentProfileLoadSnapshot(
                    p.UserId,
                    p.FullName,
                    p.Email,
                    p.IsAvailable,
                    p.MaxConcurrentTickets,
                    p.LastAssignedAt,
                    p.EfficiencyScore,
                    activeCount,
                    activeWeighted);
            })
            .ToList();
    }

    public async Task<List<AgentLoadSnapshot>> GetAssignableAgentsWithActiveLoadAsync()
    {
        var profiles = await _context.AgentProfiles
            .Where(a => a.IsAvailable && a.User.Role == UserRole.Agent)
            .Select(a => new
            {
                a.UserId,
                a.MaxConcurrentTickets,
                a.LastAssignedAt,
                a.EfficiencyScore
            })
            .ToListAsync();

        if (profiles.Count == 0)
            return [];

        var userIds = profiles.Select(p => p.UserId).ToHashSet();

        var activeTickets = await _context.Tickets
            .Where(t =>
                t.AssignedToUserId.HasValue &&
                userIds.Contains(t.AssignedToUserId.Value) &&
                (t.Status == TicketStatus.Open ||
                 t.Status == TicketStatus.Assigned ||
                 t.Status == TicketStatus.InProgress))
            .Select(t => new
            {
                UserId = t.AssignedToUserId!.Value,
                t.Priority
            })
            .ToListAsync();

        var loadByUserId = activeTickets
            .GroupBy(t => t.UserId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Count = g.Count(),
                    Weighted = g.Sum(x => x.Priority switch
                    {
                        TicketPriority.Low => 1,
                        TicketPriority.Medium => 2,
                        TicketPriority.High => 3,
                        _ => 5
                    })
                });

        return profiles
            .Select(p =>
            {
                var hasLoad = loadByUserId.TryGetValue(p.UserId, out var load);
                var activeCount = hasLoad ? load!.Count : 0;
                var activeWeighted = hasLoad ? load!.Weighted : 0;

                return new AgentLoadSnapshot(
                    p.UserId,
                    p.MaxConcurrentTickets,
                    p.LastAssignedAt,
                    p.EfficiencyScore,
                    activeCount,
                    activeWeighted);
            })
            .Where(a => a.ActiveTicketCount < a.MaxConcurrentTickets)
            .ToList();
    }

    public async Task AddAsync(AgentProfile profile)
        => await _context.AgentProfiles.AddAsync(profile);

    public void Update(AgentProfile profile)
        => _context.AgentProfiles.Update(profile);

    private static int GetPriorityWeight(TicketPriority priority)
        => priority switch
        {
            TicketPriority.Low => 1,
            TicketPriority.Medium => 2,
            TicketPriority.High => 3,
            _ => 5
        };
}
