namespace Ticketing.Application.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Role { get; }
    string? Email { get; }
}
