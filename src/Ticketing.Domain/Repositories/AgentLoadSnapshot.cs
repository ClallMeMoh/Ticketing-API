namespace Ticketing.Domain.Repositories;

public sealed record AgentLoadSnapshot(
    Guid UserId,
    int MaxConcurrentTickets,
    DateTime? LastAssignedAt,
    double EfficiencyScore,
    int ActiveTicketCount,
    int ActiveWeightedLoad);
