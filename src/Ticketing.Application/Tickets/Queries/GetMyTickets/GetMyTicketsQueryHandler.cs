using MediatR;
using Ticketing.Application.Common;
using Ticketing.Application.Interfaces;
using Ticketing.Application.Tickets.DTOs;

namespace Ticketing.Application.Tickets.Queries.GetMyTickets;

public class GetMyTicketsQueryHandler : IRequestHandler<GetMyTicketsQuery, PagedResponse<TicketResponse>>
{
    private readonly ITicketReadService _ticketReadService;
    private readonly ICurrentUserService _currentUser;

    public GetMyTicketsQueryHandler(ITicketReadService ticketReadService, ICurrentUserService currentUser)
    {
        _ticketReadService = ticketReadService;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<TicketResponse>> Handle(GetMyTicketsQuery request, CancellationToken cancellationToken)
    {
        return await _ticketReadService.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            createdByUserId: _currentUser.UserId);
    }
}
