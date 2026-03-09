using MediatR;
using Ticketing.Application.Comments.DTOs;

namespace Ticketing.Application.Comments.Queries.GetCommentsByTicketId;

public record GetCommentsByTicketIdQuery(Guid TicketId, int PageNumber = 1, int PageSize = 20)
    : IRequest<Common.PagedResponse<CommentResponse>>;
