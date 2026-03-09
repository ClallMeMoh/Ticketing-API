using MediatR;
using Ticketing.Application.Common;
using Ticketing.Application.Tickets.DTOs;
using Ticketing.Domain.Enums;

namespace Ticketing.Application.Tickets.Queries.GetTickets;

public record GetTicketsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    Guid? AssignedToUserId = null,
    Guid? CreatedByUserId = null,
    string? TitleSearch = null) : IRequest<PagedResponse<TicketResponse>>;
