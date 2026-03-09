using MediatR;
using Ticketing.Application.Exceptions;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Repositories;

namespace Ticketing.Application.Tickets.Commands.ChangeTicketStatus;

public class ChangeTicketStatusCommandHandler : IRequestHandler<ChangeTicketStatusCommand>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ChangeTicketStatusCommandHandler(
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(ChangeTicketStatusCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        var isOwner = ticket.CreatedByUserId == _currentUser.UserId;
        var isAdminOrAgent = _currentUser.Role is nameof(UserRole.Admin) or nameof(UserRole.Agent);

        if (!isOwner && !isAdminOrAgent)
            throw new ForbiddenAccessException("You do not have permission to change this ticket's status.");

        ticket.ChangeStatus(request.NewStatus);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
