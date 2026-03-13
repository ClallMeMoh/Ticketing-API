using MediatR;
using Ticketing.Application.Agents.DTOs;
using Ticketing.Application.Exceptions;
using Ticketing.Domain.Repositories;

namespace Ticketing.Application.Agents.Queries.GetAgentProfileByUserId;

public class GetAgentProfileByUserIdQueryHandler : IRequestHandler<GetAgentProfileByUserIdQuery, AgentProfileDto>
{
    private readonly IAgentProfileRepository _agentProfileRepository;

    public GetAgentProfileByUserIdQueryHandler(IAgentProfileRepository agentProfileRepository)
    {
        _agentProfileRepository = agentProfileRepository;
    }

    public async Task<AgentProfileDto> Handle(GetAgentProfileByUserIdQuery request, CancellationToken cancellationToken)
    {
        var profile = await _agentProfileRepository.GetByUserIdWithLoadAsync(request.UserId)
            ?? throw new NotFoundException("AgentProfile", request.UserId);

        return new AgentProfileDto(
            profile.UserId,
            profile.FullName,
            profile.Email,
            profile.IsAvailable,
            profile.MaxConcurrentTickets,
            profile.LastAssignedAt,
            profile.EfficiencyScore,
            profile.ActiveTicketCount,
            profile.ActiveWeightedLoad);
    }
}
