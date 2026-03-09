namespace Ticketing.Application.Auth.DTOs;

public record AuthResponse(string Token, string Email, string FullName, string Role);
