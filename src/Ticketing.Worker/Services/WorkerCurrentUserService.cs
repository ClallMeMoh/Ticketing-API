using Ticketing.Application.Interfaces;

namespace Ticketing.Worker.Services;

public sealed class WorkerCurrentUserService : ICurrentUserService
{
    public Guid UserId { get; } = Guid.Empty;
    public string Role { get; } = "System";
    public string? Email { get; } = "worker@system.local";
}
