using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Repositories;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly ApplicationDbContext _context;

    public TicketRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id)
        => await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Ticket?> GetByIdWithCommentsAsync(Guid id)
        => await _context.Tickets
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<List<Guid>> GetOpenUnassignedOlderThanAsync(DateTime olderThanUtc, int take)
        => await _context.Tickets
            .Where(t => t.Status == TicketStatus.Open &&
                        t.AssignedToUserId == null &&
                        t.CreatedAt <= olderThanUtc)
            .OrderBy(t => t.CreatedAt)
            .Select(t => t.Id)
            .Take(take)
            .ToListAsync();

    public async Task AddAsync(Ticket ticket)
        => await _context.Tickets.AddAsync(ticket);

    public void Update(Ticket ticket)
        => _context.Tickets.Update(ticket);

    public void Delete(Ticket ticket)
        => _context.Tickets.Remove(ticket);
}
