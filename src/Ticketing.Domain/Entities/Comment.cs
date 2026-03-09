using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class Comment : AuditableEntity
{
    public Guid TicketId { get; private set; }
    public Guid UserId { get; private set; }
    public string Content { get; private set; } = default!;

    public Ticket Ticket { get; private set; } = default!;
    public AppUser User { get; private set; } = default!;

    private Comment() { }

    public Comment(Guid ticketId, Guid userId, string content)
    {
        TicketId = ticketId;
        UserId = userId;
        Content = content;
    }
}
