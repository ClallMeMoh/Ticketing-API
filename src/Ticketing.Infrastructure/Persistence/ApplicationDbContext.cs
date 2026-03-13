using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Common;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Repositories;

namespace Ticketing.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IUnitOfWork
{
    private readonly TimeProvider _timeProvider;

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<AgentProfile> AgentProfiles => Set<AgentProfile>();
    public DbSet<TicketAssignmentHistory> AssignmentHistories => Set<TicketAssignmentHistory>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, TimeProvider timeProvider)
        : base(options)
    {
        _timeProvider = timeProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.Id))
                    .HasDefaultValueSql("NEWSEQUENTIALID()");
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
