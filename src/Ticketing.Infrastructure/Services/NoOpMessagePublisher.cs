using Ticketing.Application.Events;
using Ticketing.Application.Interfaces;

namespace Ticketing.Infrastructure.Services;

public sealed class NoOpMessagePublisher : IMessagePublisher
{
    public Task PublishTicketCreatedAsync(TicketCreatedEvent message, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
