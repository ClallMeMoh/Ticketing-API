using MediatR;
using Ticketing.Domain.Enums;

namespace Ticketing.Application.Tickets.Commands.UpdateTicket;

public record UpdateTicketCommand(
    Guid TicketId,
    string Title,
    string Description,
    TicketPriority Priority) : IRequest;
