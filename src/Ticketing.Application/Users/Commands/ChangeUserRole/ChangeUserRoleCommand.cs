using MediatR;
using Ticketing.Domain.Enums;

namespace Ticketing.Application.Users.Commands.ChangeUserRole;

public record ChangeUserRoleCommand(Guid UserId, UserRole Role) : IRequest;
