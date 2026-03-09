using MediatR;
using Ticketing.Application.Common;
using Ticketing.Application.Interfaces;
using Ticketing.Application.Tickets.DTOs;

namespace Ticketing.Application.Tickets.Queries.GetTickets;

public class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, PagedResponse<TicketResponse>>
{
    private readonly ITicketReadService _ticketReadService;

    public GetTicketsQueryHandler(ITicketReadService ticketReadService)
    {
        _ticketReadService = ticketReadService;
    }

    public async Task<PagedResponse<TicketResponse>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        return await _ticketReadService.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.Status,
            request.Priority,
            request.AssignedToUserId,
            request.CreatedByUserId,
            request.TitleSearch);
    }
}
