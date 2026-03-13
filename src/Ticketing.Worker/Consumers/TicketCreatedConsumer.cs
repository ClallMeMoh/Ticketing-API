using MassTransit;
using MediatR;
using Ticketing.Application.Events;
using Ticketing.Application.Tickets.Commands.AutoAssignTicket;

namespace Ticketing.Worker.Consumers;

public class TicketCreatedConsumer(
    IMediator mediator,
    ILogger<TicketCreatedConsumer> logger) : IConsumer<TicketCreatedEvent>
{
    public async Task Consume(ConsumeContext<TicketCreatedEvent> context)
    {
        await mediator.Send(
            new AutoAssignTicketCommand(context.Message.TicketId, "ticket-created-event"),
            context.CancellationToken);

        logger.LogInformation(
            "Dispatched auto-assignment for ticket {TicketId} from queue message.",
            context.Message.TicketId);
    }
}
