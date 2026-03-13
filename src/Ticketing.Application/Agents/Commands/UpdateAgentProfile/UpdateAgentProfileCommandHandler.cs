using MediatR;
using Ticketing.Application.Exceptions;
using Ticketing.Domain.Repositories;

namespace Ticketing.Application.Agents.Commands.UpdateAgentProfile;

public class UpdateAgentProfileCommandHandler : IRequestHandler<UpdateAgentProfileCommand>
{
    private readonly IAgentProfileRepository _agentProfileRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAgentProfileCommandHandler(
        IAgentProfileRepository agentProfileRepository,
        IUnitOfWork unitOfWork)
    {
        _agentProfileRepository = agentProfileRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateAgentProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _agentProfileRepository.GetByUserIdAsync(request.UserId)
            ?? throw new NotFoundException("AgentProfile", request.UserId);

        profile.UpdateAvailability(request.IsAvailable);
        profile.UpdateMaxCapacity(request.MaxConcurrentTickets);
        profile.UpdateEfficiencyScore(request.EfficiencyScore);

        _agentProfileRepository.Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
