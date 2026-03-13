using Ticketing.Domain.Entities;

namespace Ticketing.Tests.Domain;

public class AgentProfileTests
{
    private AgentProfile CreateProfile() => new(Guid.NewGuid());

    [Fact]
    public void Constructor_SetsDefaults()
    {
        var profile = CreateProfile();

        Assert.True(profile.IsAvailable);
        Assert.Equal(1.0, profile.EfficiencyScore);
        Assert.Equal(5, profile.MaxConcurrentTickets);
    }

    [Fact]
    public void Constructor_WithCustomCapacity_SetsMaxConcurrentTickets()
    {
        var profile = new AgentProfile(Guid.NewGuid(), 8);

        Assert.Equal(8, profile.MaxConcurrentTickets);
    }

    [Fact]
    public void UpdateMaxCapacity_WhenLessThanOne_ThrowsArgumentException()
    {
        var profile = CreateProfile();

        Assert.Throws<ArgumentException>(() => profile.UpdateMaxCapacity(0));
    }

    [Fact]
    public void UpdateMaxCapacity_WhenValid_UpdatesValue()
    {
        var profile = CreateProfile();

        profile.UpdateMaxCapacity(10);

        Assert.Equal(10, profile.MaxConcurrentTickets);
    }

    [Fact]
    public void UpdateAvailability_SetsValue()
    {
        var profile = CreateProfile();

        profile.UpdateAvailability(false);

        Assert.False(profile.IsAvailable);
    }

    [Fact]
    public void UpdateEfficiencyScore_WhenOutOfRange_ThrowsArgumentException()
    {
        var profile = CreateProfile();

        Assert.Throws<ArgumentException>(() => profile.UpdateEfficiencyScore(3.0));
    }

    [Fact]
    public void UpdateEfficiencyScore_WhenValid_UpdatesScore()
    {
        var profile = CreateProfile();

        profile.UpdateEfficiencyScore(1.5);

        Assert.Equal(1.5, profile.EfficiencyScore);
    }

    [Fact]
    public void RecordAssignment_SetsLastAssignedAt()
    {
        var profile = CreateProfile();
        var before = DateTime.UtcNow;

        profile.RecordAssignment();

        Assert.NotNull(profile.LastAssignedAt);
        Assert.True(profile.LastAssignedAt >= before);
    }
}
