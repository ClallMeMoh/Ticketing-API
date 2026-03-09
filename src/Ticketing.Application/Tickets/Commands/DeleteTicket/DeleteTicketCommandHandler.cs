using MediatR;
using Ticketing.Application.Exceptions;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Repositories;

namespace Ticketing.Application.Tickets.Commands.DeleteTicket;

public class DeleteTicketCommandHandler : IRequestHandler<DeleteTicketCommand>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeleteTicketCommandHandler(
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteTicketCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != nameof(UserRole.Admin))
            throw new ForbiddenAccessException("Only admins can delete tickets.");

        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        _ticketRepository.Delete(ticket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
