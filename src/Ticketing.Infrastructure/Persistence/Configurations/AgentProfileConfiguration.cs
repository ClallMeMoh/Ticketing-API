using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistence.Configurations;

public class AgentProfileConfiguration : IEntityTypeConfiguration<AgentProfile>
{
    public void Configure(EntityTypeBuilder<AgentProfile> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.UserId).IsUnique();

        builder.Property(a => a.MaxConcurrentTickets).IsRequired();
        builder.Property(a => a.IsAvailable).IsRequired();
        builder.Property(a => a.EfficiencyScore).IsRequired();

        builder.HasOne(a => a.User)
            .WithOne()
            .HasForeignKey<AgentProfile>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
