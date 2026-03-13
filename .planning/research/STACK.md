# Technology Stack

**Project:** Ticket Management System — Auto-Assignment Worker Extension
**Researched:** 2026-03-13
**Confidence:** MEDIUM — versions verified against csproj files in existing codebase; RabbitMQ/MassTransit versions sourced from training data (August 2025 cutoff) and flagged accordingly.

---

## Context: Existing Stack

The codebase already runs on **.NET 10** (not .NET 8 — the .csproj files confirm `net10.0`). This is the authoritative baseline. All new packages must target net10.0 and be compatible with existing dependency versions.

| Existing Package | Version | Role |
|------------------|---------|------|
| MediatR | 14.1.0 | CQRS pipeline |
| FluentValidation | 12.1.1 | Input validation pipeline |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.3 | ORM |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.3 | Auth |
| Swashbuckle.AspNetCore | 10.1.4 | Swagger UI |
| BCrypt.Net-Next | 4.1.0 | Password hashing |

---

## Recommended Stack for the New Milestone

### RabbitMQ Client

**Use: MassTransit.RabbitMQ**

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| MassTransit | 8.x (latest 8.x stable) | Message bus abstraction over RabbitMQ | Handles connection retry, serialization, consumer registration, and topology declaration. Integrates cleanly with .NET Generic Host and DI container. |
| MassTransit.RabbitMQ | 8.x (same as above) | RabbitMQ transport for MassTransit | Purpose-built transport; supports durable exchanges, dead-letter queues, and reconnect policies out of the box. |

**Confidence: MEDIUM** — MassTransit 8.x was current and stable as of August 2025. Cannot confirm latest patch version without network access. Verify on https://www.nuget.org/packages/MassTransit.RabbitMQ before pinning.

**Why MassTransit over raw RabbitMQ.Client:**

Raw `RabbitMQ.Client` (6.x/7.x) requires manually handling:
- connection factory setup and retry logic
- exchange/queue declaration on startup
- message serialization (JSON or binary)
- consumer thread management and ACK/NACK flows
- dead-letter routing
- channel lifecycle

MassTransit provides all of this with a tested abstraction, and the integration with .NET Generic Host (`AddMassTransit` + `AddHostedService` under the hood) means the worker service startup wires itself naturally. For a system with one exchange and one queue (as constrained by PROJECT.md), MassTransit's overhead is justified by the reliability guarantees it provides on reconnect and error handling.

**Why not raw RabbitMQ.Client:**
Fine for prototype code. For a production-like project that must survive API restarts and connection drops, you want retry-with-backoff and dead-letter routing without writing it yourself.

**Why not Azure Service Bus / other brokers:**
The constraint is explicit: RabbitMQ. Docker-friendly, runs locally.

---

### Worker Service Host

**Use: Microsoft.Extensions.Hosting (Generic Host) with `Microsoft.NET.Sdk.Worker`**

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Microsoft.Extensions.Hosting | 10.0.x (built into .NET 10 SDK) | Generic Host for background service | Ships with .NET SDK. Worker project template uses `Microsoft.NET.Sdk.Worker` which provides `IHostedService` and `BackgroundService` infrastructure. |

**Confidence: HIGH** — This is part of the .NET SDK itself. No separate NuGet package needed for the host.

The new project (`Ticketing.Worker`) should be created with the Worker Service SDK:

```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

The assignment consumer and reconciliation worker both implement `IConsumer<T>` (MassTransit) and `BackgroundService` respectively. They run inside the same worker host process.

**Why a separate Worker project over an `IHostedService` in the API:**
The PROJECT.md explicitly chose this. The rationale is sound: the API can restart independently, the worker can be scaled independently, and deployment is cleaner. Sharing `Ticketing.Application` and `Ticketing.Infrastructure` as project references keeps logic DRY without coupling the processes.

---

### Shared Layer Access from Worker

The worker references existing projects directly:

| Reference | Version | Why Needed |
|-----------|---------|------------|
| Ticketing.Application (ProjectReference) | — | AssignTicketCommand, IUnitOfWork, handler registrations |
| Ticketing.Infrastructure (ProjectReference) | — | EF Core DbContext, repositories, DI setup |

**Confidence: HIGH** — This is a standard pattern for .NET multi-project solutions.

The worker DI registration reuses `AddInfrastructure(configuration)` from `Ticketing.Infrastructure.DependencyInjection`. This is correct: EF Core, repositories, and TimeProvider are all registered the same way as in the API. The worker just does not register JWT auth, Swagger, or HTTP middleware.

---

### EF Core in the Worker

The worker needs to query agent profiles and update tickets. It reuses:

| Technology | Version | Why |
|------------|---------|-----|
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.3 (match existing) | Same DB, same context. No separate read model. |

**Confidence: HIGH** — Already in Infrastructure project.

One important constraint: because the worker is a separate process (not an ASP.NET host), `IHttpContextAccessor` registration from `DependencyInjection.cs` will register but never be populated. `CurrentUserService` will return null for the worker's calls. This is expected and acceptable — the worker runs as a system actor, not on behalf of a user. Assignment commands dispatched by the worker should carry a system-level identity, not an HTTP context claim.

---

### Concurrency Token (RowVersion)

The `Ticket` entity already has `public byte[] RowVersion { get; private set; }`. The EF Core configuration for this column needs `IsRowVersion()`. When the worker assigns a ticket, EF Core will automatically include the RowVersion in the `WHERE` clause of the `UPDATE`. If two workers race, one will get a `DbUpdateConcurrencyException`. The handler must catch this and decide: retry with fresh data, or treat as already-assigned and skip.

**Confidence: HIGH** — EF Core concurrency tokens are well-established.

---

### Reconciliation Worker

**Use: `BackgroundService` with `PeriodicTimer`**

| Technology | Version | Why |
|------------|---------|-----|
| System.Threading.PeriodicTimer | .NET 6+ built-in | Cleaner than `Task.Delay` loops. Fires on schedule, skips missed ticks cleanly. |

**Confidence: HIGH** — `PeriodicTimer` ships with .NET 6+ and is present in .NET 10.

The reconciliation worker queries `Tickets WHERE Status = Open AND CreatedAt < [now - threshold]` on a configurable interval (e.g., every 5 minutes). If it finds unassigned tickets it re-dispatches the assignment logic. This is the safety net for events that were lost during a RabbitMQ downtime.

---

### RabbitMQ Docker Image

**Use: `rabbitmq:3-management`**

| Technology | Version | Why |
|------------|---------|-----|
| rabbitmq:3-management | 3-management (floating) | Includes the management UI on port 15672. Essential for local debugging. The `3` tag tracks stable RabbitMQ 3.x releases. |

**Confidence: MEDIUM** — RabbitMQ 4.x was in active development as of August 2025. Using the `3-management` tag is the safe choice for stability. If RabbitMQ 4 has shipped and is stable by the time you run this, you could use `4-management`, but verify first.

Add to `docker-compose.yml`:

```yaml
rabbitmq:
  image: rabbitmq:3-management
  ports:
    - "5672:5672"
    - "15672:15672"
  healthcheck:
    test: rabbitmq-diagnostics -q ping
    interval: 10s
    timeout: 5s
    retries: 5
    start_period: 30s
```

The worker service should `depends_on: rabbitmq: condition: service_healthy`.

---

### MassTransit Configuration for This Topology

PROJECT.md specifies: one exchange (`ticketing.events`), one queue (`ticket-assignment`).

MassTransit maps this naturally. The publish side (API) publishes a `TicketCreatedEvent` message. MassTransit creates a fanout exchange named after the message type by convention, but you can override with `Publish` configuration to use the named exchange. The consume side (Worker) declares the queue and binds it.

Relevant MassTransit configuration pattern:

```csharp
services.AddMassTransit(x =>
{
    x.AddConsumer<TicketCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("ticket-assignment", e =>
        {
            e.ConfigureConsumer<TicketCreatedConsumer>(context);
        });
    });
});
```

The API side adds only the publish half (no consumer). The worker adds the full consumer registration.

**Confidence: MEDIUM** — MassTransit RabbitMQ API has been stable at this shape since version 7/8. Verify the exact overload signatures against the installed version.

---

### What NOT to Use

| Option | Why Not |
|--------|---------|
| NServiceBus | Commercial license for production, heavy configuration, overkill for one queue |
| Rebus | Lighter than MassTransit but smaller ecosystem, less .NET Generic Host integration |
| Azure Service Bus | Requires Azure; violates local-first Docker constraint |
| Raw RabbitMQ.Client without MassTransit | Manageable for one queue but forces manual reconnect/retry/serialization/ack logic that MassTransit gives for free |
| Hangfire / Quartz.NET for the primary event flow | Job schedulers, not event-driven consumers. Use only for reconciliation if you want persistence; `PeriodicTimer` + `BackgroundService` is simpler and sufficient for a periodic safety-net sweep. |
| Separate read database (Redis, Elasticsearch) | Out of scope per PROJECT.md; SQL Server with AsNoTracking is sufficient for agent load queries |
| IHostedService in the existing API project | Violates the separation decision in PROJECT.md; harder to scale independently |

---

## Installation

### New Worker Project

```bash
dotnet new worker -n Ticketing.Worker -o src/Ticketing.Worker
dotnet sln Ticketing.sln add src/Ticketing.Worker/Ticketing.Worker.csproj
```

### NuGet Packages

**src/Ticketing.Worker/Ticketing.Worker.csproj** additions:
```bash
dotnet add src/Ticketing.Worker package MassTransit.RabbitMQ
dotnet add src/Ticketing.Worker reference src/Ticketing.Application/Ticketing.Application.csproj
dotnet add src/Ticketing.Worker reference src/Ticketing.Infrastructure/Ticketing.Infrastructure.csproj
```

**src/Ticketing.API/Ticketing.API.csproj** additions (publish side only):
```bash
dotnet add src/Ticketing.API package MassTransit.RabbitMQ
```

**Note:** Pin MassTransit.RabbitMQ to the same version in both projects. Check NuGet for the latest 8.x stable before running these commands.

---

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| Messaging library | MassTransit.RabbitMQ | Raw RabbitMQ.Client | MassTransit handles retry, reconnect, serialization, topology |
| Message broker | RabbitMQ | Azure Service Bus | Requires Azure; Docker-unfriendly for local dev |
| Worker host | Microsoft.Extensions.Hosting (Worker SDK) | IHostedService in API project | Violates separation; harder to scale |
| Reconciliation scheduler | PeriodicTimer + BackgroundService | Hangfire | Hangfire requires its own DB table, adds dependency for a simple periodic task |
| Concurrency protection | EF Core RowVersion | Pessimistic locking (SELECT FOR UPDATE) | RowVersion is already on Ticket entity, idiomatic EF Core pattern |

---

## Confidence Summary

| Area | Confidence | Notes |
|------|------------|-------|
| .NET runtime version | HIGH | Confirmed net10.0 from csproj files |
| Existing package versions | HIGH | Read directly from csproj files |
| MassTransit recommendation | MEDIUM | Stable choice; exact patch version needs NuGet verification |
| MassTransit version (8.x) | MEDIUM | Training data cutoff August 2025; verify current patch |
| RabbitMQ Docker image (3-management) | MEDIUM | Safe conservative choice; RabbitMQ 4 may be stable by now |
| Worker SDK / Generic Host | HIGH | Ships with .NET SDK, no version uncertainty |
| EF Core RowVersion concurrency | HIGH | Core EF feature, well-documented |
| PeriodicTimer | HIGH | Ships with .NET 6+, present in net10.0 |

---

## Sources

- Existing csproj files confirmed at: `src/Ticketing.Infrastructure/Ticketing.Infrastructure.csproj`, `src/Ticketing.API/Ticketing.API.csproj`, `src/Ticketing.Application/Ticketing.Application.csproj`
- MassTransit documentation (training data, verify current): https://masstransit.io/documentation/transports/rabbitmq
- .NET Worker Service documentation: https://learn.microsoft.com/en-us/dotnet/core/extensions/workers
- EF Core concurrency tokens: https://learn.microsoft.com/en-us/ef/core/saving/concurrency
- PeriodicTimer: https://learn.microsoft.com/en-us/dotnet/api/system.threading.periodictimer
