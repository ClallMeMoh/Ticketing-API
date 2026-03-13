using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Agents.Commands.CreateAgentProfile;
using Ticketing.Application.Agents.Commands.UpdateAgentProfile;
using Ticketing.Application.Agents.Queries.GetAgentProfileByUserId;
using Ticketing.Application.Agents.Queries.GetAgentProfiles;

namespace Ticketing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AgentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AgentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAgentProfilesQuery());
        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetByUserId(Guid userId)
    {
        var result = await _mediator.Send(new GetAgentProfileByUserIdQuery(userId));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAgentProfileRequest request)
    {
        await _mediator.Send(new CreateAgentProfileCommand(request.UserId, request.MaxConcurrentTickets));
        return CreatedAtAction(nameof(GetByUserId), new { userId = request.UserId }, null);
    }

    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> Update(Guid userId, UpdateAgentProfileRequest request)
    {
        await _mediator.Send(new UpdateAgentProfileCommand(
            userId,
            request.IsAvailable,
            request.MaxConcurrentTickets,
            request.EfficiencyScore));

        return NoContent();
    }
}

public record CreateAgentProfileRequest(Guid UserId, int MaxConcurrentTickets = 5);
public record UpdateAgentProfileRequest(bool IsAvailable, int MaxConcurrentTickets, double EfficiencyScore);
