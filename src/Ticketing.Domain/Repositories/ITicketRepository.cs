using Ticketing.Domain.Entities;

namespace Ticketing.Domain.Repositories;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id);
    Task<Ticket?> GetByIdWithCommentsAsync(Guid id);
    Task AddAsync(Ticket ticket);
    void Update(Ticket ticket);
    void Delete(Ticket ticket);
}
