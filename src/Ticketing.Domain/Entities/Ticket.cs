using Ticketing.Domain.Common;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Domain.Entities;

public class Ticket : AuditableEntity
{
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = default!;

    public AppUser CreatedByUser { get; private set; } = default!;
    public AppUser? AssignedToUser { get; private set; }
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    private readonly List<Comment> _comments = new();

    private Ticket() { }

    public Ticket(string title, string description, TicketPriority priority, Guid createdByUserId)
    {
        Title = title;
        Description = description;
        Priority = priority;
        Status = TicketStatus.Open;
        CreatedByUserId = createdByUserId;
    }

    public void UpdateDetails(string title, string description, TicketPriority priority)
    {
        if (Status == TicketStatus.Closed)
            throw new DomainException("Cannot update a closed ticket.");

        Title = title;
        Description = description;
        Priority = priority;
    }

    public void AssignTo(Guid agentId)
    {
        AssignedToUserId = agentId;

        if (Status == TicketStatus.Open)
            Status = TicketStatus.Assigned;
    }

    public void ChangeStatus(TicketStatus newStatus)
    {
        if (Status == TicketStatus.Closed && newStatus != TicketStatus.Open)
            throw new DomainException("A closed ticket can only be reopened.");

        if (newStatus == TicketStatus.Assigned && AssignedToUserId is null)
            throw new DomainException("Cannot move to Assigned without an assigned agent.");

        if (newStatus == TicketStatus.InProgress && AssignedToUserId is null)
            throw new DomainException("Cannot move to InProgress without an assigned agent.");

        Status = newStatus;
    }

    public void Close()
    {
        Status = TicketStatus.Closed;
    }

    public void Reopen()
    {
        if (Status != TicketStatus.Closed)
            throw new DomainException("Only closed tickets can be reopened.");

        Status = TicketStatus.Open;
        AssignedToUserId = null;
    }
}
