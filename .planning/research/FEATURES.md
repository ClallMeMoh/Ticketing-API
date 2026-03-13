# Feature Landscape

**Domain:** Ticket auto-assignment worker system for an existing .NET Ticket Management API
**Researched:** 2026-03-13
**Confidence:** HIGH (based on direct codebase analysis + domain knowledge of assignment systems)

---

## Context

The existing API already has:
- Manual ticket assignment (`AssignTicketCommand`, guarded by Admin/Agent role)
- `Ticket.AssignTo(agentId)` domain method that transitions Open → Assigned
- `AgentProfile` entity (uncommitted) with `IsAvailable`, `MaxConcurrentTickets`, `LastAssignedAt`, `EfficiencyScore`
- `TicketAssignmentHistory` entity (uncommitted) with `AssignmentType` (Manual/Auto) and `Reason`
- `IAgentProfileRepository` and `IAssignmentHistoryRepository` interfaces (uncommitted)
- `RowVersion` concurrency token on `Ticket`

The milestone adds an automated path: ticket created → RabbitMQ event → worker consumes → algorithm selects agent → ticket assigned.

---

## Table Stakes

Features without which the auto-assignment system is broken or not usable.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Publish event on ticket creation | Without this the worker never knows a ticket needs assigning; the entire pipeline is dead | Low | `CreateTicketCommandHandler` must publish `TicketCreatedEvent` after save; RabbitMQ exchange `ticketing.events`, routing key `ticket.created` |
| Worker service consuming the assignment queue | The core delivery mechanism — without a consumer, events pile up unprocessed | Medium | Separate `Ticketing.Worker` .NET project with `IHostedService`; shares Application + Infrastructure layers |
| Agent selection algorithm | Without a selection algorithm, the worker has nothing to do | Medium | Weighted load ratio: projected load = active tickets × priority weight / MaxConcurrentTickets; rank by lowest projected load, then idle time, then efficiency score |
| Priority weight constants | Algorithm requires weights to be defined and consistent | Low | Low=1, Medium=2, High=3, Critical=5 — defined as constants in Application layer, not magic numbers in the algorithm |
| Concurrency protection on assignment | Without this, two worker instances racing on the same ticket cause double-assignment corruption | Low | `RowVersion` on `Ticket` already exists; handler must catch `DbUpdateConcurrencyException` and discard (ticket is already assigned) |
| `AgentProfile` persistence | Without stored agent metadata, the algorithm has no inputs | Medium | EF Core configuration, migration, seed data for existing agents |
| Assignment history persistence | Without this, auto-assignment is opaque — no audit trail, no debugging | Low | Write `TicketAssignmentHistory` record for every auto-assignment with reason string describing algorithm outcome |
| Available agent filtering | Algorithm must only consider agents marked `IsAvailable = true` | Low | Applied as query filter in `IAgentProfileRepository`; no available agents = skip assignment, ticket stays Open |
| No-agent fallback | If no agents are available, the worker must not crash or leave the ticket in a broken state | Low | Log a warning, acknowledge the message, let reconciliation sweep catch it later |
| RabbitMQ connection resilience | Worker cannot stop processing if broker is temporarily unavailable | Medium | Use Polly retry with exponential backoff on consumer connection; do not throw unhandled on transient failures |
| Reconciliation sweep | Events can be dropped (broker restart, network partition, worker down during publish); periodic sweep is the safety net | Medium | `IHostedService` with `PeriodicTimer` querying for tickets in `Open` status older than a configurable threshold (e.g. 2 minutes); attempts assignment inline |
| Worker Docker integration | Without this, `docker-compose up` does not start the worker; local dev is broken | Low | Add `Worker` service to `docker-compose.yml` with dependency on `rabbitmq` service |

---

## Differentiators

Features that improve the system beyond the baseline but are not required for it to function.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Agent profile management API (Admin-only) | Lets admins tune capacity and availability without a DB migration; enables operational control | Low | CRUD endpoints: create profile for agent, update `MaxConcurrentTickets`, toggle `IsAvailable`, update `EfficiencyScore`; responses expose current load metrics |
| Agent load visibility endpoint | Gives admins a live view of agent workload to understand assignment behavior | Low | `GET /api/agents/load` — returns agent id, name, active ticket count, max capacity, availability, last assigned; Admin-only |
| Assignment history endpoint | Enables support leads to audit who assigned what and why | Low | `GET /api/tickets/{id}/assignments` — returns list of `TicketAssignmentHistory` for a ticket, ordered by `AssignedAt` descending; Agent + Admin access |
| Deterministic tie-breaking | Makes algorithm output predictable when two agents are equal across all weighted metrics | Low | Final fallback: order by `UserId` ascending; prevents non-deterministic behavior under concurrent assignment |
| Structured reason string on history | Makes auto-assignment decisions human-readable in logs and audit trail | Low | Reason captures the decision: `"Auto-assigned: projected_load=0.40 (rank 1 of 3 available agents)"` |
| Worker health check endpoint | Allows Docker and orchestrators to detect a stuck or unhealthy worker | Low | `GET /health` on worker's minimal HTTP listener; returns 200 if consumer is active |

---

## Anti-Features

Features to explicitly not build in this milestone.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Agent self-selection or preference routing | Adds negotiation complexity; the algorithm already handles fairness objectively | Keep algorithm unidirectional: system assigns, agent accepts by moving status to InProgress |
| Notification system (email/SMS/push) | Separate bounded concern; couples messaging infrastructure to assignment pipeline | Defer to a future milestone; assignment history provides the data source when notifications are built |
| Real-time WebSocket push on assignment | High implementation cost for low milestone value; forces WebSocket infrastructure into the API | Client polls ticket status or calls `GET /api/tickets/{id}`; status changes are visible immediately |
| Multi-queue or topic routing | One exchange, one queue is sufficient; additional topology adds operational complexity with no routing benefit at this scale | Stick to `ticketing.events` exchange → `ticket-assignment` queue |
| Assignment scoring ML or dynamic weight tuning | Over-engineering for an internal business system; static weights are explicit and auditable | Hardcode weights as named constants; revisit only if workload profiling shows systematic imbalance |
| Dead-letter queue handling | Adds broker configuration complexity; reconciliation worker covers the same failure class more simply | Let reconciliation sweep handle unprocessed tickets; log consumer errors with enough detail to replay manually |
| Agent skill-based routing | Requires a skills taxonomy, ticket categorization, and matching logic — a separate feature in its own right | Assign by load only; skill routing is a future milestone if the domain requires it |
| Separate read database for agent load queries | No read-replica setup exists; premature optimization for current scale | Single SQL Server database; use `AsNoTracking` for load queries |

---

## Feature Dependencies

```
RabbitMQ connection resilience
    ↑ required by
Worker service consuming assignment queue
    ↑ required by
Publish event on ticket creation

AgentProfile persistence
    ↑ required by
Agent selection algorithm
    ↑ required by
Worker service consuming assignment queue

Concurrency protection (RowVersion already exists)
    ↑ required by
Worker service consuming assignment queue

Assignment history persistence
    ↑ required by
Assignment history endpoint (differentiator)

Agent profile management API
    ↑ enables
Agent load visibility endpoint (differentiator)

No-agent fallback
    ↑ required by
Reconciliation sweep
```

---

## MVP Recommendation

Prioritize in this order:

1. `AgentProfile` EF Core config + migration + seeder (unblocks all algorithm work)
2. `TicketAssignmentHistory` EF Core config + migration
3. Publish `TicketCreatedEvent` from `CreateTicketCommandHandler` via an `IMessagePublisher` interface
4. Worker project with RabbitMQ consumer + agent selection algorithm
5. Reconciliation sweep as safety net inside the same Worker project
6. Worker added to `docker-compose.yml`
7. Agent profile management endpoints (Admin-only)
8. Agent load visibility endpoint

Defer:
- Assignment history endpoint: low operational urgency; history exists in DB; can be added in a follow-up
- Worker health check: nice-to-have; not required for functional correctness

---

## Complexity Notes

- The algorithm itself is low complexity once the data model is in place. The ranked comparison (projected load → idle time → efficiency) is a few LINQ `OrderBy` clauses. The risk is getting the query right in 2 queries max without N+1.
- RabbitMQ integration is medium complexity. The consumer setup, channel lifecycle, and graceful shutdown on `CancellationToken` cancellation require care. Use `RabbitMQ.Client` directly rather than MassTransit/NServiceBus — simpler, no new abstractions.
- Reconciliation is low complexity if implemented as a simple `PeriodicTimer` loop querying `Open` tickets with age threshold. The only risk is overlap with the event-driven path; `RowVersion` concurrency protection handles this.
- Docker integration is low complexity — add a `worker` service entry to `docker-compose.yml` with `depends_on: [rabbitmq, db]`.

---

## Sources

- Direct codebase analysis: `Ticket.cs`, `AgentProfile.cs`, `TicketAssignmentHistory.cs`, `IAgentProfileRepository.cs`, `IAssignmentHistoryRepository.cs`, `AssignTicketCommandHandler.cs`, `CreateTicketCommandHandler.cs`
- `.planning/PROJECT.md` — milestone scope and constraints
- Domain knowledge of ticket assignment systems (Zendesk, Jira Service Management, Freshdesk patterns) — MEDIUM confidence (training data, not verified against current docs)
- Weighted load-balancing algorithm pattern — HIGH confidence (standard operations research, well-established)
