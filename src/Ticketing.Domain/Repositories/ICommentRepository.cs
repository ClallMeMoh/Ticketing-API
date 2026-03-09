using Ticketing.Domain.Entities;

namespace Ticketing.Domain.Repositories;

public interface ICommentRepository
{
    Task AddAsync(Comment comment);
}
