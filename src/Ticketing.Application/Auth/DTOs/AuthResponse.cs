namespace Ticketing.Application.Auth.DTOs;

public record AuthResponse(Guid UserId, string Token, string Email, string FullName, string Role);
