using MediatR;
using Ticketing.Application.Auth.DTOs;

namespace Ticketing.Application.Auth.Commands.Login;

public record LoginUserCommand(string Email, string Password) : IRequest<AuthResponse>;
