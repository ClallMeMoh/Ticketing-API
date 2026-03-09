using MediatR;
using Ticketing.Domain.Enums;

namespace Ticketing.Application.Tickets.Commands.ChangeTicketStatus;

public record ChangeTicketStatusCommand(Guid TicketId, TicketStatus NewStatus) : IRequest;
