using MediatR;

namespace Ticketing.Application.Agents.Commands.UpdateAgentProfile;

public record UpdateAgentProfileCommand(
    Guid UserId,
    bool IsAvailable,
    int MaxConcurrentTickets,
    double EfficiencyScore) : IRequest;
