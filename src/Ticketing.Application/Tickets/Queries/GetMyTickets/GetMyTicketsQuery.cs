using MediatR;
using Ticketing.Application.Common;
using Ticketing.Application.Tickets.DTOs;

namespace Ticketing.Application.Tickets.Queries.GetMyTickets;

public record GetMyTicketsQuery(int PageNumber = 1, int PageSize = 10) : IRequest<PagedResponse<TicketResponse>>;
