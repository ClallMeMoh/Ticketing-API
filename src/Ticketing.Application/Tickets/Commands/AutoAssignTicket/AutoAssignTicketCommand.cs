using MediatR;

namespace Ticketing.Application.Tickets.Commands.AutoAssignTicket;

public record AutoAssignTicketCommand(Guid TicketId, string Trigger) : IRequest;
