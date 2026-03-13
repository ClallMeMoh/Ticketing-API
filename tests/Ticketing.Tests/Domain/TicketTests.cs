using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Tests.Domain;

public class TicketTests
{
    private Ticket CreateTicket() =>
        new("Test Ticket", "Description", TicketPriority.Medium, Guid.NewGuid());

    [Fact]
    public void Constructor_SetsStatusToOpen()
    {
        var ticket = CreateTicket();
        Assert.Equal(TicketStatus.Open, ticket.Status);
    }

    [Fact]
    public void UpdateDetails_WhenOpen_UpdatesFields()
    {
        var ticket = CreateTicket();
        ticket.UpdateDetails("New Title", "New Description", TicketPriority.High);

        Assert.Equal("New Title", ticket.Title);
        Assert.Equal("New Description", ticket.Description);
        Assert.Equal(TicketPriority.High, ticket.Priority);
    }

    [Fact]
    public void UpdateDetails_WhenClosed_ThrowsDomainException()
    {
        var ticket = CreateTicket();
        ticket.Close();

        Assert.Throws<DomainException>(() =>
            ticket.UpdateDetails("Title", "Desc", TicketPriority.Low));
    }

    [Fact]
    public void AssignTo_WhenOpen_SetsAssigned()
    {
        var ticket = CreateTicket();
        var agentId = Guid.NewGuid();

        ticket.AssignTo(agentId);

        Assert.Equal(agentId, ticket.AssignedToUserId);
        Assert.Equal(TicketStatus.Assigned, ticket.Status);
    }

    [Fact]
    public void AssignTo_WhenAlreadyAssigned_KeepsStatus()
    {
        var ticket = CreateTicket();
        ticket.AssignTo(Guid.NewGuid());

        var newAgentId = Guid.NewGuid();
        ticket.AssignTo(newAgentId);

        Assert.Equal(newAgentId, ticket.AssignedToUserId);
        Assert.Equal(TicketStatus.Assigned, ticket.Status);
    }

    [Fact]
    public void ChangeStatus_ToInProgress_WithoutAgent_ThrowsDomainException()
    {
        var ticket = CreateTicket();

        Assert.Throws<DomainException>(() =>
            ticket.ChangeStatus(TicketStatus.InProgress));
    }

    [Fact]
    public void ChangeStatus_ToInProgress_WithAgent_Succeeds()
    {
        var ticket = CreateTicket();
        ticket.AssignTo(Guid.NewGuid());

        ticket.ChangeStatus(TicketStatus.Resolved);
        ticket.ChangeStatus(TicketStatus.InProgress);

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
    }

    [Fact]
    public void ChangeStatus_WhenClosed_OnlyAllowsReopen()
    {
        var ticket = CreateTicket();
        ticket.Close();

        Assert.Throws<DomainException>(() =>
            ticket.ChangeStatus(TicketStatus.InProgress));
    }

    [Fact]
    public void ChangeStatus_WhenClosed_CanReopenViaChangeStatus()
    {
        var ticket = CreateTicket();
        ticket.Close();

        ticket.ChangeStatus(TicketStatus.Open);

        Assert.Equal(TicketStatus.Open, ticket.Status);
    }

    [Fact]
    public void Close_SetsStatusToClosed()
    {
        var ticket = CreateTicket();
        ticket.Close();
        Assert.Equal(TicketStatus.Closed, ticket.Status);
    }

    [Fact]
    public void Reopen_WhenClosed_SetsOpenAndClearsAssignment()
    {
        var ticket = CreateTicket();
        ticket.AssignTo(Guid.NewGuid());
        ticket.Close();

        ticket.Reopen();

        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Null(ticket.AssignedToUserId);
    }

    [Fact]
    public void Reopen_WhenNotClosed_ThrowsDomainException()
    {
        var ticket = CreateTicket();

        Assert.Throws<DomainException>(() => ticket.Reopen());
    }

    [Fact]
    public void AssignTo_WhenInProgress_KeepsInProgressStatus()
    {
        var ticket = CreateTicket();
        ticket.AssignTo(Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.InProgress);

        var newAgentId = Guid.NewGuid();
        ticket.AssignTo(newAgentId);

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Equal(newAgentId, ticket.AssignedToUserId);
    }

    [Fact]
    public void ChangeStatus_WhenClosed_CannotTransitionToAssigned()
    {
        var ticket = CreateTicket();
        ticket.AssignTo(Guid.NewGuid());
        ticket.Close();

        Assert.Throws<DomainException>(() => ticket.ChangeStatus(TicketStatus.Assigned));
    }

    [Fact]
    public void ChangeStatus_WhenResolved_CanTransitionToAssigned()
    {
        var ticket = CreateTicket();
        ticket.AssignTo(Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Resolved);

        ticket.ChangeStatus(TicketStatus.Assigned);

        Assert.Equal(TicketStatus.Assigned, ticket.Status);
    }
}
