using MediatR;
using Ticketing.Application.Exceptions;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Repositories;

namespace Ticketing.Application.Tickets.Commands.AssignTicket;

public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AssignTicketCommandHandler(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(AssignTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        var agent = await _userRepository.GetByIdAsync(request.AgentId)
            ?? throw new NotFoundException("User", request.AgentId);

        if (agent.Role is not (UserRole.Admin or UserRole.Agent))
            throw new ForbiddenAccessException("Tickets can only be assigned to agents or admins.");

        ticket.AssignTo(agent.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
