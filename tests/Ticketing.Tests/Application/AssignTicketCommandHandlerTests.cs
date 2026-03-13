using NSubstitute;
using Ticketing.Application.Exceptions;
using Ticketing.Application.Interfaces;
using Ticketing.Application.Tickets.Commands.AssignTicket;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Repositories;

namespace Ticketing.Tests.Application;

public class AssignTicketCommandHandlerTests
{
    private readonly ITicketRepository _ticketRepository = Substitute.For<ITicketRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IAssignmentHistoryRepository _historyRepository = Substitute.For<IAssignmentHistoryRepository>();
    private readonly AssignTicketCommandHandler _handler;

    public AssignTicketCommandHandlerTests()
    {
        _handler = new AssignTicketCommandHandler(_ticketRepository, _userRepository, _unitOfWork, _currentUser, _historyRepository);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAdminOrAgent_ThrowsForbidden()
    {
        _currentUser.Role.Returns(nameof(UserRole.User));
        var command = new AssignTicketCommand(Guid.NewGuid(), Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenTargetIsNotAgent_ThrowsForbidden()
    {
        _currentUser.Role.Returns(nameof(UserRole.Admin));
        _currentUser.Email.Returns("admin@test.com");
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var ticket = new Ticket("Test", "Desc", TicketPriority.Medium, Guid.NewGuid());
        _ticketRepository.GetByIdAsync(ticketId).Returns(ticket);

        var regularUser = new AppUser("Regular User", "user@test.com", "hash", UserRole.User);
        _userRepository.GetByIdAsync(userId).Returns(regularUser);

        var command = new AssignTicketCommand(ticketId, userId);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenValid_AssignsTicket()
    {
        _currentUser.Role.Returns(nameof(UserRole.Admin));
        _currentUser.Email.Returns("admin@test.com");
        var ticketId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var ticket = new Ticket("Test", "Desc", TicketPriority.Medium, Guid.NewGuid());
        _ticketRepository.GetByIdAsync(ticketId).Returns(ticket);

        var agent = new AppUser("Agent", "agent@test.com", "hash", UserRole.Agent);
        _userRepository.GetByIdAsync(agentId).Returns(agent);

        var command = new AssignTicketCommand(ticketId, agentId);
        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesAssignmentHistoryRecord()
    {
        _currentUser.Role.Returns(nameof(UserRole.Admin));
        _currentUser.Email.Returns("admin@test.com");
        var ticketId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var ticket = new Ticket("Test", "Desc", TicketPriority.Medium, Guid.NewGuid());
        _ticketRepository.GetByIdAsync(ticketId).Returns(ticket);

        var agent = new AppUser("Agent", "agent@test.com", "hash", UserRole.Agent);
        _userRepository.GetByIdAsync(agentId).Returns(agent);

        var command = new AssignTicketCommand(ticketId, agentId);
        await _handler.Handle(command, CancellationToken.None);

        await _historyRepository.Received(1).AddAsync(
            Arg.Is<TicketAssignmentHistory>(h =>
                h.AssignmentType == AssignmentType.Manual &&
                h.Reason.Contains("admin@test.com")));
    }

    [Fact]
    public async Task Handle_WhenValid_HistoryReasonContainsAssignerEmail()
    {
        _currentUser.Role.Returns(nameof(UserRole.Admin));
        _currentUser.Email.Returns("admin@test.com");
        var ticketId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var ticket = new Ticket("Test", "Desc", TicketPriority.Medium, Guid.NewGuid());
        _ticketRepository.GetByIdAsync(ticketId).Returns(ticket);

        var agent = new AppUser("Agent", "agent@test.com", "hash", UserRole.Agent);
        _userRepository.GetByIdAsync(agentId).Returns(agent);

        var command = new AssignTicketCommand(ticketId, agentId);
        await _handler.Handle(command, CancellationToken.None);

        await _historyRepository.Received(1).AddAsync(
            Arg.Is<TicketAssignmentHistory>(h =>
                h.Reason == $"Manually assigned by admin@test.com"));
    }
}
