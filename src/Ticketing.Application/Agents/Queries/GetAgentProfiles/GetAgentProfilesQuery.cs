using MediatR;
using Ticketing.Application.Agents.DTOs;

namespace Ticketing.Application.Agents.Queries.GetAgentProfiles;

public record GetAgentProfilesQuery : IRequest<List<AgentProfileDto>>;
