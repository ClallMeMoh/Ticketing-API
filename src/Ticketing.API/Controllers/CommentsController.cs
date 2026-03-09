using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Comments.Commands.AddComment;
using Ticketing.Application.Comments.Queries.GetCommentsByTicketId;

namespace Ticketing.API.Controllers;

[ApiController]
[Route("api/tickets/{ticketId:guid}/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Add(Guid ticketId, AddCommentRequest request)
    {
        var command = new AddCommentCommand(ticketId, request.Content);
        var commentId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetByTicketId), new { ticketId }, new { id = commentId });
    }

    [HttpGet]
    public async Task<IActionResult> GetByTicketId(
        Guid ticketId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetCommentsByTicketIdQuery(ticketId, pageNumber, pageSize));
        return Ok(result);
    }
}

public record AddCommentRequest(string Content);
