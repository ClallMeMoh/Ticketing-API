using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Tickets.Commands.AssignTicket;
using Ticketing.Application.Tickets.Commands.ChangeTicketStatus;
using Ticketing.Application.Tickets.Commands.CreateTicket;
using Ticketing.Application.Tickets.Commands.DeleteTicket;
using Ticketing.Application.Tickets.Commands.UpdateTicket;
using Ticketing.Application.Tickets.Queries.GetAssignedTickets;
using Ticketing.Application.Tickets.Queries.GetMyTickets;
using Ticketing.Application.Tickets.Queries.GetTicketById;
using Ticketing.Application.Tickets.Queries.GetTickets;
using Ticketing.Domain.Enums;

namespace Ticketing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetTicketByIdQuery(id));
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] Guid? assignedToUserId = null,
        [FromQuery] Guid? createdByUserId = null,
        [FromQuery] string? titleSearch = null)
    {
        var query = new GetTicketsQuery(pageNumber, pageSize, status, priority, assignedToUserId, createdByUserId, titleSearch);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new GetMyTicketsQuery(pageNumber, pageSize));
        return Ok(result);
    }

    [HttpGet("assigned")]
    public async Task<IActionResult> GetAssigned([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new GetAssignedTicketsQuery(pageNumber, pageSize));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTicketCommand command)
    {
        var ticketId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = ticketId }, new { id = ticketId });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTicketRequest request)
    {
        var command = new UpdateTicketCommand(id, request.Title, request.Description, request.Priority);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPut("{id:guid}/assign")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> Assign(Guid id, AssignTicketRequest request)
    {
        var command = new AssignTicketCommand(id, request.AgentId);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeTicketStatusRequest request)
    {
        var command = new ChangeTicketStatusCommand(id, request.NewStatus);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteTicketCommand(id));
        return NoContent();
    }
}

public record UpdateTicketRequest(string Title, string Description, TicketPriority Priority);
public record AssignTicketRequest(Guid AgentId);
public record ChangeTicketStatusRequest(TicketStatus NewStatus);
