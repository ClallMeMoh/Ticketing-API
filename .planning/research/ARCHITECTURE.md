# Architecture Patterns: RabbitMQ Worker Integration

**Domain:** Auto-assignment worker for .NET DDD/CQRS ticket management API
**Researched:** 2026-03-13
**Confidence:** HIGH — based on direct codebase analysis of the existing system

---

## Context

The existing system is a 4-layer DDD API:

```
Ticketing.Domain        (entities, enums, repository interfaces — no external dependencies)
Ticketing.Application   (MediatR commands/queries, handlers, validators, app interfaces)
Ticketing.Infrastructure (EF Core, SQL Server, repository impls, JWT, password hashing)
Ticketing.API           (ASP.NET Core controllers, middleware, startup)
```

The new milestone adds a worker service as a **fifth deployable unit** that shares layers with the existing API rather than duplicating them.

---

## Recommended Architecture

### System Overview

```
┌────────────────────────────────────────────────┐
│                   HTTP Clients                 │
└──────────────────────┬─────────────────────────┘
                       │ HTTP
┌──────────────────────▼─────────────────────────┐
│              Ticketing.API                     │
│  (Controllers, Middleware, Swagger, JWT Auth)  │
│                                                │
│  CreateTicketCommandHandler                    │
│    → saves Ticket                              │
│    → publishes TicketCreatedEvent              │
└──────────┬─────────────────────────────────────┘
           │
           │  IMessagePublisher.Publish(TicketCreatedEvent)
           │
┌──────────▼─────────────────────────────────────┐
│              RabbitMQ                          │
│  Exchange: ticketing.events (fanout/direct)    │
│  Queue:    ticket-assignment                   │
└──────────┬─────────────────────────────────────┘
           │
           │  AMQP consume
           │
┌──────────▼─────────────────────────────────────┐
│         Ticketing.Worker                       │
│  (IHostedService / BackgroundService)          │
│                                                │
│  TicketAssignmentConsumer                      │
│    → IMediator.Send(AutoAssignTicketCommand)   │
└──────────┬─────────────────────────────────────┘
           │
           │  shared project references
           │
┌──────────▼─────────────────────────────────────┐
│         Ticketing.Application                  │
│  AutoAssignTicketCommandHandler                │
│    → ranking algorithm                         │
│    → Ticket.AssignTo()                         │
│    → TicketAssignmentHistory insert            │
│    → AgentProfile.RecordAssignment()           │
└──────────┬─────────────────────────────────────┘
           │
           │  ITicketRepository, IAgentProfileRepository, IUnitOfWork
           │
┌──────────▼─────────────────────────────────────┐
│         Ticketing.Infrastructure               │
│  ApplicationDbContext, Repositories            │
│  RabbitMqPublisher (implements IMessagePublisher)│
│  RabbitMqConsumerService (BackgroundService)   │
└──────────┬─────────────────────────────────────┘
           │
┌──────────▼─────────────────────────────────────┐
│         SQL Server                             │
│  Tickets, AgentProfiles,                       │
│  TicketAssignmentHistories, Users              │
└────────────────────────────────────────────────┘
```

---

## Component Boundaries

### Ticketing.API (existing, modified)

**Responsibility:** HTTP entry point. Modified to publish a `TicketCreatedEvent` after saving a ticket.

**Key change:** `CreateTicketCommandHandler` gains a dependency on `IMessagePublisher`. After `SaveChangesAsync`, it calls `_publisher.Publish(new TicketCreatedEvent(ticket.Id, ticket.Priority))`. This is the only touch point in the existing code.

**Communicates with:** RabbitMQ (outbound, fire-and-forget), SQL Server (via Infrastructure)

**Does not:** contain assignment logic, know about agents, or wait for assignment to complete

---

### Ticketing.Worker (new project)

**Responsibility:** Process messages from the `ticket-assignment` queue. Translate each message into a MediatR command. Nothing else.

**Project type:** `dotnet new worker` — a .NET Worker Service (console host with `IHostedService`)

**Key components:**
- `Program.cs` — configures DI, registers Application + Infrastructure layers (same extension methods as the API), starts the host
- `TicketAssignmentConsumer : BackgroundService` — connects to RabbitMQ, consumes messages in a loop, calls `IMediator.Send(AutoAssignTicketCommand)`
- No controllers, no HTTP, no Swagger

**Communicates with:** RabbitMQ (inbound), Ticketing.Application (via MediatR), Ticketing.Infrastructure (via shared DI registration)

**Does not:** contain the ranking algorithm, know about HTTP, or duplicate any Application/Infrastructure code

---

### Ticketing.Application (existing, extended)

**New additions:**
- `AutoAssignTicketCommand` — carries `TicketId`
- `AutoAssignTicketCommandHandler` — contains the ranking algorithm, calls domain methods, records history
- `IMessagePublisher` interface — declared here so Application has no RabbitMQ dependency
- `IAssignmentHistoryRepository` interface (already exists as a file in the repo)
- Agent ranking logic (pure C# — no infrastructure dependency)

**Ranking algorithm lives here**, not in the worker and not in the infrastructure. This keeps it testable and dependency-free.

**Communicates with:** Domain (entities, repository interfaces), Infrastructure (via DI at runtime)

---

### Ticketing.Infrastructure (existing, extended)

**New additions:**
- `RabbitMqPublisher : IMessagePublisher` — wraps RabbitMQ.Client, publishes to `ticketing.events` exchange
- `AgentProfileRepository : IAgentProfileRepository`
- `AssignmentHistoryRepository : IAssignmentHistoryRepository`
- EF Core configurations for `AgentProfile` and `TicketAssignmentHistory`
- New migration for the new tables and `RowVersion` column on `Ticket`
- `DependencyInjection.cs` extended to register new repositories and `IMessagePublisher`

**The RabbitMQ consumer itself (connection loop) belongs in the Worker project**, not Infrastructure. Infrastructure provides the publisher (fire-and-forget) and the database repositories. The Worker owns the long-lived consumer connection.

---

### Ticketing.Domain (existing, extended)

**New additions (already partially present as uncommitted files):**
- `AgentProfile` entity — availability, capacity, efficiency score, last assigned timestamp
- `TicketAssignmentHistory` entity — records every assignment with type (Manual/Auto) and reason
- `AssignmentType` enum — Manual, Auto
- `IAgentProfileRepository` interface
- `IAssignmentHistoryRepository` interface
- `TicketStatus.Assigned` value — already present in current code

**No messaging dependencies.** Domain stays pure.

---

### Reconciliation Worker (inside Ticketing.Worker)

A second `BackgroundService` registered in the Worker host. Runs on a fixed interval (e.g., every 5 minutes). Queries for tickets in `Open` status older than a threshold. For each, sends `AutoAssignTicketCommand` directly, bypassing RabbitMQ. This is a safety net — not the primary path.

**Key design choice:** Reconciliation reuses `AutoAssignTicketCommandHandler` — no duplicate assignment logic.

---

## Data Flow

### Primary Path: Ticket Created → Auto-Assigned

```
1. POST /api/tickets
   → TicketsController.Create()
   → IMediator.Send(CreateTicketCommand)
   → CreateTicketCommandHandler
       → new Ticket(title, description, priority, userId)
       → ITicketRepository.AddAsync(ticket)
       → IUnitOfWork.SaveChangesAsync()        ← ticket persisted with Status=Open
       → IMessagePublisher.Publish(              ← event published AFTER successful save
           TicketCreatedEvent { TicketId, Priority })
   → 201 Created { ticketId }

2. RabbitMQ delivers TicketCreatedEvent to ticket-assignment queue

3. TicketAssignmentConsumer (BackgroundService in Ticketing.Worker)
   → deserializes message → TicketCreatedEvent
   → IMediator.Send(AutoAssignTicketCommand { TicketId })

4. AutoAssignTicketCommandHandler
   → ITicketRepository.GetByIdAsync(ticketId)
       → returns null or already-assigned ticket → exit early (idempotent)
   → IAgentProfileRepository.GetAvailableAgentsWithLoadAsync()
       → single query: available agents + their current active ticket count
   → ranking algorithm:
       projectedLoad = (activeTickets + priorityWeight) / maxConcurrentTickets
       sort by: projectedLoad ASC → LastAssignedAt ASC → EfficiencyScore DESC → Id ASC
   → ticket.AssignTo(bestAgent.UserId)    ← domain method, sets Status=Assigned
   → agentProfile.RecordAssignment()
   → new TicketAssignmentHistory(ticketId, agentId, AssignmentType.Auto, reason)
   → IAssignmentHistoryRepository.AddAsync(history)
   → IUnitOfWork.SaveChangesAsync()       ← all changes in one transaction
   → ack message to RabbitMQ

5. On failure:
   → nack message → RabbitMQ requeues (up to configured retry limit)
   → After max retries → dead-letter queue (optional, configured in RabbitMQ)
```

### Secondary Path: Reconciliation Sweep

```
ReconciliationBackgroundService (runs every N minutes)
→ ITicketReadService.GetUnassignedOpenTicketsOlderThan(threshold)
→ for each ticket:
    IMediator.Send(AutoAssignTicketCommand { TicketId })
    → same handler as primary path (idempotent check at top)
```

### Manual Assignment Path (existing, unchanged)

```
PUT /api/tickets/{id}/assign
→ AssignTicketCommand
→ AssignTicketCommandHandler
    → ticket.AssignTo(agentId)            ← domain method
    → IUnitOfWork.SaveChangesAsync()
    (no TicketAssignmentHistory in current code — should be added in this milestone)
```

---

## Patterns to Follow

### Pattern 1: Publish After Commit

**What:** Publish the RabbitMQ event only after `SaveChangesAsync()` succeeds.

**Why:** If the database write fails, no event should be sent. The reverse (event sent, then DB fails) causes an unassigned ticket with no event ever retried.

**Implementation:**
```csharp
await _unitOfWork.SaveChangesAsync(cancellationToken);
await _publisher.Publish(new TicketCreatedEvent(ticket.Id, ticket.Priority));
```

The small window between DB commit and publish is acceptable here — the reconciliation worker catches any tickets that slip through.

---

### Pattern 2: Idempotent Handler

**What:** `AutoAssignTicketCommandHandler` checks if the ticket is already assigned before running the algorithm.

**Why:** The same message may be delivered more than once (RabbitMQ at-least-once delivery). The reconciliation sweep may also send a command for a ticket already handled by the primary path.

**Implementation:**
```csharp
var ticket = await _ticketRepository.GetByIdAsync(command.TicketId);
if (ticket is null || ticket.AssignedToUserId.HasValue) return;
```

---

### Pattern 3: RowVersion Concurrency on Ticket

**What:** `Ticket.RowVersion` byte array used as EF Core concurrency token.

**Why:** If two worker instances (or a manual assignment racing with auto-assignment) both read and then write the same ticket, EF Core throws `DbUpdateConcurrencyException` on the second save.

**Handling:** Catch `DbUpdateConcurrencyException` in the handler, nack the message, let it retry. On retry, the idempotency check at the top will find the ticket already assigned and exit cleanly.

---

### Pattern 4: Worker Shares Existing DI Extensions

**What:** `Ticketing.Worker/Program.cs` calls the same `services.AddApplication()` and `services.AddInfrastructure(configuration)` extension methods used by the API.

**Why:** No duplication of repository or handler registration. Single source of truth for how dependencies are wired.

**What the Worker does NOT register:** JWT authentication, Swagger, HTTP context accessor, `ICurrentUserService` (no HTTP context exists in the worker).

---

### Pattern 5: IMessagePublisher Interface in Application Layer

**What:** Declare the publisher interface in `Ticketing.Application.Interfaces`:
```csharp
public interface IMessagePublisher
{
    Task Publish<T>(T message, CancellationToken cancellationToken = default);
}
```

**Why:** Keeps Application layer free of RabbitMQ dependencies. Infrastructure implements it. This is consistent with how `ICurrentUserService`, `IJwtTokenGenerator`, and `IPasswordHasher` are handled in the existing code.

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: Assignment Logic in the Consumer

**What:** Putting the agent ranking algorithm directly in `TicketAssignmentConsumer`.

**Why bad:** The consumer is infrastructure — it handles AMQP protocol. Business logic in infrastructure cannot be unit tested cleanly, breaks the DDD layer rule, and makes the algorithm unavailable to the reconciliation path without duplication.

**Instead:** Consumer sends `IMediator.Send(AutoAssignTicketCommand)`. Handler contains the algorithm.

---

### Anti-Pattern 2: Publishing Events Inside the Domain Entity

**What:** Having `Ticket` raise domain events that automatically trigger publishing.

**Why bad:** Domain event dispatch infrastructure (outbox, mediator, message broker) adds significant complexity. Overkill for this project scope. The PROJECT.md explicitly calls out avoiding unnecessary complexity.

**Instead:** `CreateTicketCommandHandler` explicitly publishes after save. Simple, traceable, obvious.

---

### Anti-Pattern 3: Separate DbContext for the Worker

**What:** Creating a second EF Core DbContext or connection string for the worker.

**Why bad:** Unnecessary. The worker references `Ticketing.Infrastructure` and uses the same `ApplicationDbContext` via the same `AddInfrastructure()` registration. Worker just needs a connection string in its own `appsettings.json`.

**Instead:** Shared Infrastructure project. Single DbContext. Same connection string (configured separately per service in docker-compose).

---

### Anti-Pattern 4: Blocking Ticket Creation on Assignment

**What:** Running the assignment algorithm synchronously inside `CreateTicketCommandHandler` before returning the response.

**Why bad:** Assignment requires querying all available agents — adding latency to every ticket creation. If assignment fails, ticket creation fails. Tight coupling between creation and assignment.

**Instead:** Async decoupling via RabbitMQ. Creation returns 201 immediately. Assignment happens within seconds as a separate concern.

---

### Anti-Pattern 5: N+1 Queries in the Ranking Algorithm

**What:** Loading all available agents, then querying each agent's ticket count in a loop.

**Why bad:** With 50 agents, that is 51 queries per assignment. PROJECT.md constraint: "2 queries max, no N+1 loops."

**Instead:** Single query joining AgentProfiles with a subquery/GROUP BY on active tickets:
```sql
SELECT ap.*, COUNT(t.Id) as ActiveTickets
FROM AgentProfiles ap
LEFT JOIN Tickets t ON t.AssignedToUserId = ap.UserId
    AND t.Status IN (1, 2)   -- Assigned, InProgress
WHERE ap.IsAvailable = 1
GROUP BY ap.*
```

---

## Component Dependency Graph (Extended)

```
Ticketing.Worker
├── Ticketing.Application   (MediatR handlers, AutoAssignTicketCommand)
│   └── Ticketing.Domain    (entities, enums, repository interfaces)
└── Ticketing.Infrastructure (DbContext, repositories, RabbitMqPublisher)
    └── Ticketing.Application
        └── Ticketing.Domain

Ticketing.API
├── Ticketing.Application
│   └── Ticketing.Domain
└── Ticketing.Infrastructure
    └── Ticketing.Application
        └── Ticketing.Domain
```

Both `Ticketing.API` and `Ticketing.Worker` reference `Ticketing.Application` and `Ticketing.Infrastructure`. Neither references the other.

---

## Suggested Build Order

Dependencies drive the order. Each phase builds on the previous.

| Step | What | Why First |
|------|------|-----------|
| 1 | Domain extensions: `AgentProfile`, `TicketAssignmentHistory`, `IAgentProfileRepository`, `IAssignmentHistoryRepository`, `AssignmentType` | All other layers depend on these types |
| 2 | Infrastructure: EF configs, repository impls, migration | Application handlers need concrete implementations available at test time |
| 3 | Application: `IMessagePublisher` interface, `AutoAssignTicketCommand` + handler, ranking algorithm | Worker and API both depend on this layer |
| 4 | Modify `CreateTicketCommandHandler` to publish `TicketCreatedEvent` | Depends on `IMessagePublisher` interface existing |
| 5 | Infrastructure: `RabbitMqPublisher` implementing `IMessagePublisher` | Depends on interface from step 3 |
| 6 | `Ticketing.Worker` project: `Program.cs`, `TicketAssignmentConsumer`, `ReconciliationBackgroundService` | Depends on all Application and Infrastructure work being done |
| 7 | Docker: update `docker-compose.yml` to add RabbitMQ service and Worker service | Depends on Worker project existing |
| 8 | API endpoints: agent profile CRUD, agent load visibility | Can be parallelized with steps 6-7 if needed |

---

## Scalability Considerations

| Concern | Current Scope (1 worker instance) | If Scaling Out |
|---------|-----------------------------------|----------------|
| Double-assignment | RowVersion on Ticket catches race conditions | Same — optimistic concurrency remains correct |
| Queue throughput | Single consumer adequate for internal business system | Add more worker instances; RabbitMQ distributes messages round-robin |
| Agent load query | Single query with GROUP BY per assignment | Add index on `Tickets(AssignedToUserId, Status)` before scaling |
| Message ordering | Not required — each ticket assigned independently | No change needed |
| Reconciliation frequency | Every 5 minutes is fine at low volume | Tune interval based on ticket volume |

---

## Sources

- Direct analysis of existing codebase (`src/` directory), confidence: HIGH
- `.planning/codebase/ARCHITECTURE.md` — existing architecture documentation, confidence: HIGH
- `.planning/PROJECT.md` — milestone requirements and constraints, confidence: HIGH
- RabbitMQ integration with .NET `IHostedService` / `BackgroundService` is a well-established pattern in .NET; training data confidence: MEDIUM (pattern stable since .NET Core 2.1)
- MediatR usage from worker services is a documented pattern; training data confidence: MEDIUM
