using MediatR;
using Ticketing.Application.Agents.DTOs;
using Ticketing.Domain.Repositories;

namespace Ticketing.Application.Agents.Queries.GetAgentProfiles;

public class GetAgentProfilesQueryHandler : IRequestHandler<GetAgentProfilesQuery, List<AgentProfileDto>>
{
    private readonly IAgentProfileRepository _agentProfileRepository;

    public GetAgentProfilesQueryHandler(IAgentProfileRepository agentProfileRepository)
    {
        _agentProfileRepository = agentProfileRepository;
    }

    public async Task<List<AgentProfileDto>> Handle(GetAgentProfilesQuery request, CancellationToken cancellationToken)
    {
        var profiles = await _agentProfileRepository.GetAllWithLoadAsync();

        return profiles.Select(p => new AgentProfileDto(
            p.UserId,
            p.FullName,
            p.Email,
            p.IsAvailable,
            p.MaxConcurrentTickets,
            p.LastAssignedAt,
            p.EfficiencyScore,
            p.ActiveTicketCount,
            p.ActiveWeightedLoad)).ToList();
    }
}
