using MediatR;
using Ticketing.Application.Agents.DTOs;

namespace Ticketing.Application.Agents.Queries.GetAgentProfileByUserId;

public record GetAgentProfileByUserIdQuery(Guid UserId) : IRequest<AgentProfileDto>;
