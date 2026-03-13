using Ticketing.Domain.Enums;

namespace Ticketing.Application.Events;

public sealed record TicketCreatedEvent(
    Guid TicketId,
    TicketPriority Priority);
