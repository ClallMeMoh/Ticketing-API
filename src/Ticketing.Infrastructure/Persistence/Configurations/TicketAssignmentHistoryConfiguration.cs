using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistence.Configurations;

public class TicketAssignmentHistoryConfiguration : IEntityTypeConfiguration<TicketAssignmentHistory>
{
    public void Configure(EntityTypeBuilder<TicketAssignmentHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(h => h.AssignmentType).IsRequired();
        builder.Property(h => h.AssignedAt).IsRequired();

        builder.HasIndex(h => h.TicketId);

        builder.HasOne(h => h.Ticket)
            .WithMany()
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.AssignedToUser)
            .WithMany()
            .HasForeignKey(h => h.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
