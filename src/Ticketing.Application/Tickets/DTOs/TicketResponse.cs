using Ticketing.Domain.Enums;

namespace Ticketing.Application.Tickets.DTOs;

public record TicketResponse(
    Guid Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    Guid CreatedByUserId,
    string CreatedByUserName,
    Guid? AssignedToUserId,
    string? AssignedToUserName,
    DateTime CreatedAt,
    DateTime UpdatedAt);
