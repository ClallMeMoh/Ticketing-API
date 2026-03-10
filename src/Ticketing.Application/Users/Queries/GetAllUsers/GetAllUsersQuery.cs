using MediatR;
using Ticketing.Application.Users.DTOs;

namespace Ticketing.Application.Users.Queries.GetAllUsers;

public record GetAllUsersQuery : IRequest<List<UserDto>>;
