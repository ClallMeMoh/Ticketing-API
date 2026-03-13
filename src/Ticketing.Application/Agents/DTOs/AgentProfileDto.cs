namespace Ticketing.Application.Agents.DTOs;

public record AgentProfileDto(
    Guid UserId,
    string FullName,
    string Email,
    bool IsAvailable,
    int MaxConcurrentTickets,
    DateTime? LastAssignedAt,
    double EfficiencyScore,
    int ActiveTicketCount,
    int ActiveWeightedLoad);
