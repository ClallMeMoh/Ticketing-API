using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class AgentProfile : AuditableEntity
{
    public Guid UserId { get; private set; }
    public bool IsAvailable { get; private set; }
    public int MaxConcurrentTickets { get; private set; }
    public DateTime? LastAssignedAt { get; private set; }
    public double EfficiencyScore { get; private set; }

    public AppUser User { get; private set; } = default!;

    private AgentProfile() { }

    public AgentProfile(Guid userId, int maxConcurrentTickets = 5)
    {
        UserId = userId;
        IsAvailable = true;
        MaxConcurrentTickets = maxConcurrentTickets;
        EfficiencyScore = 1.0;
    }

    public void UpdateAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
    }

    public void UpdateMaxCapacity(int maxConcurrentTickets)
    {
        if (maxConcurrentTickets < 1)
            throw new ArgumentException("Max concurrent tickets must be at least 1.");

        MaxConcurrentTickets = maxConcurrentTickets;
    }

    public void RecordAssignment()
    {
        LastAssignedAt = DateTime.UtcNow;
    }

    public void UpdateEfficiencyScore(double score)
    {
        if (score < 0 || score > 2)
            throw new ArgumentException("Efficiency score must be between 0 and 2.");

        EfficiencyScore = score;
    }
}
