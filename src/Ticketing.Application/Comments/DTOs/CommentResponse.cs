namespace Ticketing.Application.Comments.DTOs;

public record CommentResponse(
    Guid Id,
    Guid TicketId,
    Guid UserId,
    string UserName,
    string Content,
    DateTime CreatedAt);
