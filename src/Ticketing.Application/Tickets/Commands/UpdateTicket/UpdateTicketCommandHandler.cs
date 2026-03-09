using MediatR;
using Ticketing.Application.Exceptions;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Repositories;

namespace Ticketing.Application.Tickets.Commands.UpdateTicket;

public class UpdateTicketCommandHandler : IRequestHandler<UpdateTicketCommand>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateTicketCommandHandler(
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        var isOwner = ticket.CreatedByUserId == _currentUser.UserId;
        var isAdminOrAgent = _currentUser.Role is nameof(UserRole.Admin) or nameof(UserRole.Agent);

        if (!isOwner && !isAdminOrAgent)
            throw new ForbiddenAccessException("You do not have permission to update this ticket.");

        ticket.UpdateDetails(request.Title, request.Description, request.Priority);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
