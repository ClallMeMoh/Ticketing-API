using MediatR;

namespace Ticketing.Application.Tickets.Commands.DeleteTicket;

public record DeleteTicketCommand(Guid TicketId) : IRequest;
