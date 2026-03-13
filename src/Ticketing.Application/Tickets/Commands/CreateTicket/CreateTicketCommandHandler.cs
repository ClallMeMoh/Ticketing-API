using MediatR;
using Ticketing.Application.Events;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Repositories;

namespace Ticketing.Application.Tickets.Commands.CreateTicket;

public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Guid>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IMessagePublisher _messagePublisher;

    public CreateTicketCommandHandler(
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IMessagePublisher messagePublisher)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _messagePublisher = messagePublisher;
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
        await _messagePublisher.PublishTicketCreatedAsync(
            new TicketCreatedEvent(ticket.Id, ticket.Priority),
            cancellationToken);

        return ticket.Id;
    }
}
