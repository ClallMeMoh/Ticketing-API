using MediatR;

namespace Ticketing.Application.Agents.Commands.CreateAgentProfile;

public record CreateAgentProfileCommand(Guid UserId, int MaxConcurrentTickets) : IRequest;
