using MediatR;
using Ticketing.Application.Tickets.Commands.AutoAssignTicket;
using Ticketing.Domain.Repositories;

namespace Ticketing.Worker.Services;

public class ReconciliationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ReconciliationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = configuration.GetValue<int?>("Worker:ReconciliationIntervalSeconds") ?? 60;
        var ageThresholdSeconds = configuration.GetValue<int?>("Worker:ReconciliationTicketAgeThresholdSeconds") ?? 60;
        var batchSize = configuration.GetValue<int?>("Worker:ReconciliationBatchSize") ?? 50;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var ticketRepository = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var olderThanUtc = DateTime.UtcNow.AddSeconds(-ageThresholdSeconds);
                var ticketIds = await ticketRepository.GetOpenUnassignedOlderThanAsync(olderThanUtc, batchSize);

                foreach (var ticketId in ticketIds)
                {
                    await mediator.Send(
                        new AutoAssignTicketCommand(ticketId, "reconciliation-sweep"),
                        stoppingToken);
                }

                if (ticketIds.Count > 0)
                {
                    logger.LogInformation(
                        "Reconciliation dispatched auto-assignment for {Count} tickets.",
                        ticketIds.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reconciliation sweep failed. Worker continues running.");
            }
        }
    }
}
