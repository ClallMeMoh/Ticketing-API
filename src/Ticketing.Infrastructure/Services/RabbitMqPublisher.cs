using MassTransit;
using Ticketing.Application.Events;
using Ticketing.Application.Interfaces;

namespace Ticketing.Infrastructure.Services;

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public RabbitMqPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishTicketCreatedAsync(TicketCreatedEvent message, CancellationToken cancellationToken = default)
    {
        await _publishEndpoint.Publish(message, cancellationToken);
    }
}
