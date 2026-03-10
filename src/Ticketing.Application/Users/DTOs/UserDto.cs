namespace Ticketing.Application.Users.DTOs;

public record UserDto(Guid Id, string FullName, string Email, string Role);
