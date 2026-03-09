using MediatR;

namespace Ticketing.Application.Tickets.Commands.AssignTicket;

public record AssignTicketCommand(Guid TicketId, Guid AgentId) : IRequest;
