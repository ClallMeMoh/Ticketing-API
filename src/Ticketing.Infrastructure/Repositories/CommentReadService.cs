using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Comments.DTOs;
using Ticketing.Application.Common;
using Ticketing.Application.Interfaces;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Repositories;

public class CommentReadService : ICommentReadService
{
    private readonly ApplicationDbContext _context;

    public CommentReadService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<CommentResponse>> GetPagedByTicketIdAsync(
        Guid ticketId, int pageNumber, int pageSize)
    {
        var query = _context.Comments
            .AsNoTracking()
            .Where(c => c.TicketId == ticketId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CommentResponse(
                c.Id,
                c.TicketId,
                c.UserId,
                c.User != null ? c.User.FullName : string.Empty,
                c.Content,
                c.CreatedAt))
            .ToListAsync();

        return new PagedResponse<CommentResponse>(items, totalCount, pageNumber, pageSize);
    }
}
