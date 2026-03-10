using MediatR;
using Ticketing.Application.Users.DTOs;
using Ticketing.Domain.Repositories;

namespace Ticketing.Application.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, List<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetAllUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync();

        return users.Select(u => new UserDto(u.Id, u.FullName, u.Email, u.Role.ToString())).ToList();
    }
}
