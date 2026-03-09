using MediatR;
using Ticketing.Application.Auth.DTOs;

namespace Ticketing.Application.Auth.Commands.Register;

public record RegisterUserCommand(
    string FullName,
    string Email,
    string Password) : IRequest<AuthResponse>;
