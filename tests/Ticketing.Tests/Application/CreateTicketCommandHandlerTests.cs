using NSubstitute;
using Ticketing.Application.Events;
using Ticketing.Application.Interfaces;
using Ticketing.Application.Tickets.Commands.CreateTicket;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Repositories;

namespace Ticketing.Tests.Application;

public class CreateTicketCommandHandlerTests
{
    private readonly ITicketRepository _ticketRepository = Substitute.For<ITicketRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IMessagePublisher _messagePublisher = Substitute.For<IMessagePublisher>();
    private readonly CreateTicketCommandHandler _handler;

    public CreateTicketCommandHandlerTests()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _handler = new CreateTicketCommandHandler(_ticketRepository, _unitOfWork, _currentUser, _messagePublisher);
    }

    [Fact]
    public async Task Handle_CreatesTicketAndSaves()
    {
        var command = new CreateTicketCommand("Test", "Description", TicketPriority.High);

        await _handler.Handle(command, CancellationToken.None);

        await _ticketRepository.Received(1).AddAsync(Arg.Is<Ticket>(t =>
            t.Title == "Test" &&
            t.Description == "Description" &&
            t.Priority == TicketPriority.High));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _messagePublisher.Received(1)
            .PublishTicketCreatedAsync(Arg.Is<TicketCreatedEvent>(e => e.Priority == TicketPriority.High), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PublishesEventAfterSave()
    {
        var command = new CreateTicketCommand("Test", "Description", TicketPriority.Medium);

        await _handler.Handle(command, CancellationToken.None);

        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _messagePublisher.PublishTicketCreatedAsync(Arg.Any<TicketCreatedEvent>(), Arg.Any<CancellationToken>());
        });
    }
}
