using MediatR;
using Ticketing.Application.Common;
using Ticketing.Application.Interfaces;
using Ticketing.Application.Tickets.DTOs;

namespace Ticketing.Application.Tickets.Queries.GetAssignedTickets;

public class GetAssignedTicketsQueryHandler : IRequestHandler<GetAssignedTicketsQuery, PagedResponse<TicketResponse>>
{
    private readonly ITicketReadService _ticketReadService;
    private readonly ICurrentUserService _currentUser;

    public GetAssignedTicketsQueryHandler(ITicketReadService ticketReadService, ICurrentUserService currentUser)
    {
        _ticketReadService = ticketReadService;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<TicketResponse>> Handle(GetAssignedTicketsQuery request, CancellationToken cancellationToken)
    {
        return await _ticketReadService.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            assignedToUserId: _currentUser.UserId);
    }
}
