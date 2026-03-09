using MediatR;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Repositories;

namespace Ticketing.Application.Tickets.Commands.CreateTicket;

public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Guid>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateTicketCommandHandler(
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = new Ticket(
            request.Title,
            request.Description,
            request.Priority,
            _currentUser.UserId);

        await _ticketRepository.AddAsync(ticket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ticket.Id;
    }
}
