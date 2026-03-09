using Ticketing.Application.Common;
using Ticketing.Application.Tickets.DTOs;
using Ticketing.Domain.Enums;

namespace Ticketing.Application.Interfaces;

public interface ITicketReadService
{
    Task<TicketResponse?> GetByIdAsync(Guid id);

    Task<PagedResponse<TicketResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        TicketStatus? status = null,
        TicketPriority? priority = null,
        Guid? assignedToUserId = null,
        Guid? createdByUserId = null,
        string? titleSearch = null);
}
