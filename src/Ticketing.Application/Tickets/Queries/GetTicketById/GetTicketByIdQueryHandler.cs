using MediatR;
using Ticketing.Application.Exceptions;
using Ticketing.Application.Interfaces;
using Ticketing.Application.Tickets.DTOs;

namespace Ticketing.Application.Tickets.Queries.GetTicketById;

public class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, TicketResponse>
{
    private readonly ITicketReadService _ticketReadService;

    public GetTicketByIdQueryHandler(ITicketReadService ticketReadService)
    {
        _ticketReadService = ticketReadService;
    }

    public async Task<TicketResponse> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        return await _ticketReadService.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Ticket", request.TicketId);
    }
}
