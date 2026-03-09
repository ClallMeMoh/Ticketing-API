using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Auth.Commands.Login;
using Ticketing.Application.Auth.Commands.Register;

namespace Ticketing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginUserCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
