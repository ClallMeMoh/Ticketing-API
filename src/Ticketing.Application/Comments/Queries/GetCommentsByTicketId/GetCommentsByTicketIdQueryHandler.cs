using MediatR;
using Ticketing.Application.Comments.DTOs;
using Ticketing.Application.Common;
using Ticketing.Application.Interfaces;

namespace Ticketing.Application.Comments.Queries.GetCommentsByTicketId;

public class GetCommentsByTicketIdQueryHandler
    : IRequestHandler<GetCommentsByTicketIdQuery, PagedResponse<CommentResponse>>
{
    private readonly ICommentReadService _commentReadService;

    public GetCommentsByTicketIdQueryHandler(ICommentReadService commentReadService)
    {
        _commentReadService = commentReadService;
    }

    public async Task<PagedResponse<CommentResponse>> Handle(
        GetCommentsByTicketIdQuery request, CancellationToken cancellationToken)
    {
        return await _commentReadService.GetPagedByTicketIdAsync(
            request.TicketId, request.PageNumber, request.PageSize);
    }
}
