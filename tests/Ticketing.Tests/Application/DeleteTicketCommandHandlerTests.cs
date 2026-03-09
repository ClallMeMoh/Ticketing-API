using NSubstitute;
using Ticketing.Application.Exceptions;
using Ticketing.Application.Interfaces;
using Ticketing.Application.Tickets.Commands.DeleteTicket;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Repositories;

namespace Ticketing.Tests.Application;

public class DeleteTicketCommandHandlerTests
{
    private readonly ITicketRepository _ticketRepository = Substitute.For<ITicketRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly DeleteTicketCommandHandler _handler;

    public DeleteTicketCommandHandlerTests()
    {
        _handler = new DeleteTicketCommandHandler(_ticketRepository, _unitOfWork, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenNotAdmin_ThrowsForbidden()
    {
        _currentUser.Role.Returns(nameof(UserRole.User));
        var command = new DeleteTicketCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenTicketNotFound_ThrowsNotFound()
    {
        _currentUser.Role.Returns(nameof(UserRole.Admin));
        _ticketRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Ticket?)null);

        var command = new DeleteTicketCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenAdmin_DeletesTicket()
    {
        _currentUser.Role.Returns(nameof(UserRole.Admin));
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket("Test", "Desc", TicketPriority.Low, Guid.NewGuid());
        _ticketRepository.GetByIdAsync(ticketId).Returns(ticket);

        var command = new DeleteTicketCommand(ticketId);
        await _handler.Handle(command, CancellationToken.None);

        _ticketRepository.Received(1).Delete(ticket);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
