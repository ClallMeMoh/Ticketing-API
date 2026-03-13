namespace Ticketing.Domain.Repositories;

public sealed record AgentProfileLoadSnapshot(
    Guid UserId,
    string FullName,
    string Email,
    bool IsAvailable,
    int MaxConcurrentTickets,
    DateTime? LastAssignedAt,
    double EfficiencyScore,
    int ActiveTicketCount,
    int ActiveWeightedLoad);
