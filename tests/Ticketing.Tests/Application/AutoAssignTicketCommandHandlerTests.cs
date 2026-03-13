using NSubstitute;
using Ticketing.Application.Tickets.Commands.AutoAssignTicket;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Ticketing.Tests.Application;

public class AutoAssignTicketCommandHandlerTests
{
    private readonly ITicketRepository _ticketRepository = Substitute.For<ITicketRepository>();
    private readonly IAgentProfileRepository _agentProfileRepository = Substitute.For<IAgentProfileRepository>();
    private readonly IAssignmentHistoryRepository _historyRepository = Substitute.For<IAssignmentHistoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<AutoAssignTicketCommandHandler> _logger = Substitute.For<ILogger<AutoAssignTicketCommandHandler>>();
    private readonly AutoAssignTicketCommandHandler _handler;

    public AutoAssignTicketCommandHandlerTests()
    {
        _handler = new AutoAssignTicketCommandHandler(
            _ticketRepository,
            _agentProfileRepository,
            _historyRepository,
            _unitOfWork,
            _logger);
    }

    [Fact]
    public async Task Handle_WhenTicketDoesNotExist_DoesNothing()
    {
        var command = new AutoAssignTicketCommand(Guid.NewGuid(), "ticket-created-event");

        await _handler.Handle(command, CancellationToken.None);

        await _historyRepository.DidNotReceive().AddAsync(Arg.Any<TicketAssignmentHistory>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTicketAlreadyAssigned_DoesNothing()
    {
        var ticket = new Ticket("Test", "Desc", TicketPriority.Medium, Guid.NewGuid());
        ticket.AssignTo(Guid.NewGuid());
        _ticketRepository.GetByIdAsync(ticket.Id).Returns(ticket);

        var command = new AutoAssignTicketCommand(ticket.Id, "ticket-created-event");
        await _handler.Handle(command, CancellationToken.None);

        await _agentProfileRepository.DidNotReceive().GetAssignableAgentsWithActiveLoadAsync();
        await _historyRepository.DidNotReceive().AddAsync(Arg.Any<TicketAssignmentHistory>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoEligibleAgents_LeavesTicketOpen()
    {
        var ticket = new Ticket("Test", "Desc", TicketPriority.High, Guid.NewGuid());
        _ticketRepository.GetByIdAsync(ticket.Id).Returns(ticket);
        _agentProfileRepository.GetAssignableAgentsWithActiveLoadAsync().Returns(new List<AgentLoadSnapshot>());

        var command = new AutoAssignTicketCommand(ticket.Id, "ticket-created-event");
        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Null(ticket.AssignedToUserId);
        await _historyRepository.DidNotReceive().AddAsync(Arg.Any<TicketAssignmentHistory>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEligibleAgentsExist_AssignsBestAgentAndWritesHistory()
    {
        var ticket = new Ticket("Test", "Desc", TicketPriority.Critical, Guid.NewGuid());
        _ticketRepository.GetByIdAsync(ticket.Id).Returns(ticket);

        var lowerProjected = new AgentLoadSnapshot(
            Guid.NewGuid(),
            8,
            DateTime.UtcNow.AddMinutes(-5),
            1.2,
            3,
            6);

        var higherProjected = new AgentLoadSnapshot(
            Guid.NewGuid(),
            5,
            DateTime.UtcNow.AddHours(-1),
            1.8,
            1,
            4);

        _agentProfileRepository.GetAssignableAgentsWithActiveLoadAsync()
            .Returns(new List<AgentLoadSnapshot> { higherProjected, lowerProjected });

        var command = new AutoAssignTicketCommand(ticket.Id, "ticket-created-event");
        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        Assert.Equal(lowerProjected.UserId, ticket.AssignedToUserId);

        await _historyRepository.Received(1).AddAsync(Arg.Is<TicketAssignmentHistory>(h =>
            h.AssignmentType == AssignmentType.Auto &&
            h.AssignedToUserId == lowerProjected.UserId &&
            h.Reason.Contains("weighted-load") &&
            h.Reason.Contains("ticket-created-event")));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenConcurrentUpdateOccurs_DoesNotThrow()
    {
        var ticket = new Ticket("Test", "Desc", TicketPriority.Medium, Guid.NewGuid());
        _ticketRepository.GetByIdAsync(ticket.Id).Returns(ticket);

        _agentProfileRepository.GetAssignableAgentsWithActiveLoadAsync()
            .Returns(new List<AgentLoadSnapshot>
            {
                new(Guid.NewGuid(), 5, null, 1.0, 0, 0)
            });

        _unitOfWork
            .When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ => throw new DbUpdateConcurrencyException("Simulated concurrency."));

        var command = new AutoAssignTicketCommand(ticket.Id, "ticket-created-event");
        var exception = await Record.ExceptionAsync(() => _handler.Handle(command, CancellationToken.None));

        Assert.Null(exception);
    }

    private sealed class DbUpdateConcurrencyException(string message) : Exception(message);
}
