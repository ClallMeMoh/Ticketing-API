using Ticketing.Application.Comments.DTOs;
using Ticketing.Application.Common;

namespace Ticketing.Application.Interfaces;

public interface ICommentReadService
{
    Task<PagedResponse<CommentResponse>> GetPagedByTicketIdAsync(
        Guid ticketId, int pageNumber, int pageSize);
}
