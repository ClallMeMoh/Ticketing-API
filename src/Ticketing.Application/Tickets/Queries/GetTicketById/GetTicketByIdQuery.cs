using MediatR;
using Ticketing.Application.Tickets.DTOs;

namespace Ticketing.Application.Tickets.Queries.GetTicketById;

public record GetTicketByIdQuery(Guid TicketId) : IRequest<TicketResponse>;
