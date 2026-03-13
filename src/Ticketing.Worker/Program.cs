using MassTransit;
using Ticketing.Application;
using Ticketing.Application.Interfaces;
using Ticketing.Infrastructure;
using Ticketing.Infrastructure.Services;
using Ticketing.Worker.Consumers;
using Ticketing.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddWorkerInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ICurrentUserService, WorkerCurrentUserService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, WorkerJwtTokenGenerator>();
builder.Services.AddMessaging(
    builder.Configuration,
    configureConsumers: x => x.AddConsumer<TicketCreatedConsumer>(),
    includeAssignmentEndpoint: true);
builder.Services.AddHostedService<ReconciliationBackgroundService>();

var host = builder.Build();
host.Run();
