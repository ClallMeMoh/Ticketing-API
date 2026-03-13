using NSubstitute;
using Ticketing.Application.Agents.Commands.CreateAgentProfile;
using Ticketing.Application.Agents.Commands.UpdateAgentProfile;
using Ticketing.Application.Exceptions;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Repositories;

namespace Ticketing.Tests.Application;

public class AgentProfileCommandHandlersTests
{
    private readonly IAgentProfileRepository _agentProfileRepository = Substitute.For<IAgentProfileRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Create_WhenUserIsAgent_CreatesProfile()
    {
        var user = new AppUser("Agent", "agent@test.com", "hash", UserRole.Agent);
        _userRepository.GetByIdAsync(user.Id).Returns(user);
        _agentProfileRepository.GetByUserIdAsync(user.Id).Returns((AgentProfile?)null);

        var handler = new CreateAgentProfileCommandHandler(_agentProfileRepository, _userRepository, _unitOfWork);
        await handler.Handle(new CreateAgentProfileCommand(user.Id, 6), CancellationToken.None);

        await _agentProfileRepository.Received(1).AddAsync(Arg.Is<AgentProfile>(p =>
            p.UserId == user.Id && p.MaxConcurrentTickets == 6));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WhenUserIsNotAgent_ThrowsForbidden()
    {
        var user = new AppUser("User", "user@test.com", "hash", UserRole.User);
        _userRepository.GetByIdAsync(user.Id).Returns(user);

        var handler = new CreateAgentProfileCommandHandler(_agentProfileRepository, _userRepository, _unitOfWork);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new CreateAgentProfileCommand(user.Id, 5), CancellationToken.None));
    }

    [Fact]
    public async Task Update_WhenProfileExists_UpdatesAndSaves()
    {
        var userId = Guid.NewGuid();
        var profile = new AgentProfile(userId, 5);
        _agentProfileRepository.GetByUserIdAsync(userId).Returns(profile);

        var handler = new UpdateAgentProfileCommandHandler(_agentProfileRepository, _unitOfWork);
        await handler.Handle(new UpdateAgentProfileCommand(userId, false, 9, 1.5), CancellationToken.None);

        Assert.False(profile.IsAvailable);
        Assert.Equal(9, profile.MaxConcurrentTickets);
        Assert.Equal(1.5, profile.EfficiencyScore);
        _agentProfileRepository.Received(1).Update(profile);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
