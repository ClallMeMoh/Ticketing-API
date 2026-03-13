using MediatR;
using Microsoft.Extensions.Logging;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Repositories;

namespace Ticketing.Application.Tickets.Commands.AutoAssignTicket;

public class AutoAssignTicketCommandHandler : IRequestHandler<AutoAssignTicketCommand>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IAgentProfileRepository _agentProfileRepository;
    private readonly IAssignmentHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AutoAssignTicketCommandHandler> _logger;

    public AutoAssignTicketCommandHandler(
        ITicketRepository ticketRepository,
        IAgentProfileRepository agentProfileRepository,
        IAssignmentHistoryRepository historyRepository,
        IUnitOfWork unitOfWork,
        ILogger<AutoAssignTicketCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _agentProfileRepository = agentProfileRepository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(AutoAssignTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId);
        if (ticket is null)
            return;

        // Idempotency guard for duplicate events/retries.
        if (ticket.AssignedToUserId is not null || ticket.Status is not TicketStatus.Open)
            return;

        var incomingWeight = GetPriorityWeight(ticket.Priority);
        var agents = await _agentProfileRepository.GetAssignableAgentsWithActiveLoadAsync();

        var selectedAgent = agents
            .OrderBy(a => (a.ActiveWeightedLoad + incomingWeight) / (double)a.MaxConcurrentTickets)
            .ThenBy(a => a.LastAssignedAt ?? DateTime.MinValue)
            .ThenByDescending(a => a.EfficiencyScore)
            .ThenBy(a => a.UserId)
            .FirstOrDefault();

        if (selectedAgent is null)
        {
            _logger.LogWarning(
                "No eligible agents found for ticket {TicketId}. Ticket remains open.",
                ticket.Id);
            return;
        }

        ticket.AssignTo(selectedAgent.UserId);

        var projectedLoadRatio = (selectedAgent.ActiveWeightedLoad + incomingWeight) /
            (double)selectedAgent.MaxConcurrentTickets;

        var reason = $"Auto-assigned via weighted-load (trigger={request.Trigger}, priority={ticket.Priority}, projectedLoadRatio={projectedLoadRatio:F4})";
        var history = new TicketAssignmentHistory(ticket.Id, selectedAgent.UserId, AssignmentType.Auto, reason);
        await _historyRepository.AddAsync(history);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            // Another worker instance assigned this ticket first.
            _logger.LogWarning(
                ex,
                "Concurrency conflict while auto-assigning ticket {TicketId}. Another process likely assigned it first.",
                ticket.Id);
        }
    }

    private static int GetPriorityWeight(TicketPriority priority)
        => priority switch
        {
            TicketPriority.Low => 1,
            TicketPriority.Medium => 2,
            TicketPriority.High => 3,
            TicketPriority.Critical => 5,
            _ => 1
        };
}
