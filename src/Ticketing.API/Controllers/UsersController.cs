using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Users.Commands.ChangeUserRole;
using Ticketing.Application.Users.Queries.GetAllUsers;
using Ticketing.Domain.Enums;

namespace Ticketing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _mediator.Send(new GetAllUsersQuery());
        return Ok(users);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, ChangeUserRoleRequest request)
    {
        var command = new ChangeUserRoleCommand(id, request.NewRole);
        await _mediator.Send(command);
        return NoContent();
    }
}

public record ChangeUserRoleRequest(UserRole NewRole);
