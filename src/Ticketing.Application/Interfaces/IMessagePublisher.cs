using Ticketing.Application.Events;

namespace Ticketing.Application.Interfaces;

public interface IMessagePublisher
{
    Task PublishTicketCreatedAsync(TicketCreatedEvent message, CancellationToken cancellationToken = default);
}
