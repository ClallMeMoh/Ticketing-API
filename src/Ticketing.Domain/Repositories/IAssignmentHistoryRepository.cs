using Ticketing.Domain.Entities;

namespace Ticketing.Domain.Repositories;

public interface IAssignmentHistoryRepository
{
    Task AddAsync(TicketAssignmentHistory history);
    Task<List<TicketAssignmentHistory>> GetByTicketIdAsync(Guid ticketId);
}
