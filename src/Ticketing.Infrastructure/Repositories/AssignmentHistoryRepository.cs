using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Repositories;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Repositories;

public class AssignmentHistoryRepository : IAssignmentHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public AssignmentHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TicketAssignmentHistory history)
        => await _context.AssignmentHistories.AddAsync(history);

    public async Task<List<TicketAssignmentHistory>> GetByTicketIdAsync(Guid ticketId)
        => await _context.AssignmentHistories
            .Where(h => h.TicketId == ticketId)
            .OrderByDescending(h => h.AssignedAt)
            .ToListAsync();
}
