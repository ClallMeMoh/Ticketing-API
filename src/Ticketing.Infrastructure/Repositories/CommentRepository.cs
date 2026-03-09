using Ticketing.Domain.Entities;
using Ticketing.Domain.Repositories;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly ApplicationDbContext _context;

    public CommentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Comment comment)
        => await _context.Comments.AddAsync(comment);
}
