using MediatR;
using Ticketing.Domain.Enums;

namespace Ticketing.Application.Tickets.Commands.CreateTicket;

public record CreateTicketCommand(
    string Title,
    string Description,
    TicketPriority Priority) : IRequest<Guid>;
