using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Common;
using Ticketing.Application.Interfaces;
using Ticketing.Application.Tickets.DTOs;
using Ticketing.Domain.Enums;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Repositories;

public class TicketReadService : ITicketReadService
{
    private readonly ApplicationDbContext _context;

    public TicketReadService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TicketResponse?> GetByIdAsync(Guid id)
    {
        return await _context.Tickets
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TicketResponse(
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.Priority,
                t.CreatedByUserId,
                t.CreatedByUser != null ? t.CreatedByUser.FullName : string.Empty,
                t.AssignedToUserId,
                t.AssignedToUser != null ? t.AssignedToUser.FullName : null,
                t.CreatedAt,
                t.UpdatedAt))
            .FirstOrDefaultAsync();
    }

    public async Task<PagedResponse<TicketResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        TicketStatus? status = null,
        TicketPriority? priority = null,
        Guid? assignedToUserId = null,
        Guid? createdByUserId = null,
        string? titleSearch = null)
    {
        var query = _context.Tickets.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        if (assignedToUserId.HasValue)
            query = query.Where(t => t.AssignedToUserId == assignedToUserId.Value);

        if (createdByUserId.HasValue)
            query = query.Where(t => t.CreatedByUserId == createdByUserId.Value);

        if (!string.IsNullOrWhiteSpace(titleSearch))
            query = query.Where(t => t.Title.Contains(titleSearch));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketResponse(
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.Priority,
                t.CreatedByUserId,
                t.CreatedByUser != null ? t.CreatedByUser.FullName : string.Empty,
                t.AssignedToUserId,
                t.AssignedToUser != null ? t.AssignedToUser.FullName : null,
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync();

        return new PagedResponse<TicketResponse>(items, totalCount, pageNumber, pageSize);
    }
}
