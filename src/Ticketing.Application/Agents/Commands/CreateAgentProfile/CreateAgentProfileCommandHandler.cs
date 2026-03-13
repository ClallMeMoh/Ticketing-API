using MediatR;
using Ticketing.Application.Exceptions;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Repositories;

namespace Ticketing.Application.Agents.Commands.CreateAgentProfile;

public class CreateAgentProfileCommandHandler : IRequestHandler<CreateAgentProfileCommand>
{
    private readonly IAgentProfileRepository _agentProfileRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAgentProfileCommandHandler(
        IAgentProfileRepository agentProfileRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _agentProfileRepository = agentProfileRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CreateAgentProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId)
            ?? throw new NotFoundException("User", request.UserId);

        if (user.Role != UserRole.Agent)
            throw new ForbiddenAccessException("Agent profile can only be created for users with Agent role.");

        var existing = await _agentProfileRepository.GetByUserIdAsync(request.UserId);
        if (existing is not null)
            throw new ForbiddenAccessException("Agent profile already exists for this user.");

        await _agentProfileRepository.AddAsync(new AgentProfile(request.UserId, request.MaxConcurrentTickets));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
