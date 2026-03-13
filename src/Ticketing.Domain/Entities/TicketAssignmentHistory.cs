using Ticketing.Domain.Common;
using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Entities;

public class TicketAssignmentHistory : BaseEntity
{
    public Guid TicketId { get; private set; }
    public Guid AssignedToUserId { get; private set; }
    public AssignmentType AssignmentType { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    public Ticket Ticket { get; private set; } = default!;
    public AppUser AssignedToUser { get; private set; } = default!;

    private TicketAssignmentHistory() { }

    public TicketAssignmentHistory(Guid ticketId, Guid assignedToUserId, AssignmentType assignmentType, string reason)
    {
        TicketId = ticketId;
        AssignedToUserId = assignedToUserId;
        AssignmentType = assignmentType;
        AssignedAt = DateTime.UtcNow;
        Reason = reason;
    }
}
