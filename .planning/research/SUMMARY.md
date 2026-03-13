# Project Research Summary

**Project:** Ticket Management System — Auto-Assignment Worker Extension
**Domain:** .NET Worker Service + RabbitMQ + Weighted Load-Balancing Auto-Assignment
**Researched:** 2026-03-13
**Confidence:** MEDIUM-HIGH

## Executive Summary

This milestone extends a functioning .NET 10 DDD/CQRS ticket management API with an asynchronous auto-assignment pipeline. The architecture adds a fifth deployable unit — `Ticketing.Worker` — that shares the existing `Ticketing.Application` and `Ticketing.Infrastructure` layers without duplicating any logic. When a ticket is created, the API publishes a `TicketCreatedEvent` to RabbitMQ after the database commit. The worker consumes this event and dispatches an `AutoAssignTicketCommand` through MediatR, which executes a weighted load-ranking algorithm to select the best available agent and persist the assignment. A separate reconciliation `BackgroundService` within the same worker process acts as a safety net for events lost during broker downtime.

The recommended approach is MassTransit 8.x over raw `RabbitMQ.Client` for the messaging layer, because MassTransit handles connection retry, dead-letter routing, and topology declaration without custom scaffolding. The existing EF Core `RowVersion` concurrency token on `Ticket` provides race-condition protection at no additional cost. The algorithm itself is straightforward LINQ ranked by projected load ratio, with idle time and efficiency score as tie-breakers. The critical design constraint is that assignment logic must live in `AutoAssignTicketCommandHandler` (Application layer), not in the consumer itself, so the reconciliation path can reuse the same handler without code duplication.

The primary risks are scoped-service DI lifetime mismatches in `BackgroundService` (a singleton host), incorrect RabbitMQ message acknowledgment order, and the existing `Ticket.RowVersion` property lacking the `.IsRowVersion()` EF Core configuration that activates optimistic concurrency. These three issues are silent — they compile and appear to work until load or failure conditions expose the corruption. All three must be addressed before any algorithm or handler code is written.

---

## Key Findings

### Recommended Stack

The existing codebase runs on .NET 10 (`net10.0` confirmed in `.csproj` files) with MediatR 14.1.0, EF Core 10.0.3, and FluentValidation 12.1.1. No version upgrades are needed. The new dependency is MassTransit 8.x (latest stable 8.x) with the `MassTransit.RabbitMQ` transport. MassTransit integrates with .NET Generic Host via `AddMassTransit`, aligning with the Worker SDK project type (`Microsoft.NET.Sdk.Worker`). The reconciliation sweep uses `PeriodicTimer` (built into .NET 6+, present in .NET 10) — no additional scheduler dependency is needed.

**Core technologies:**
- **MassTransit.RabbitMQ 8.x**: Message bus abstraction — handles retry, serialization, topology, and reconnect that raw `RabbitMQ.Client` requires manually
- **Microsoft.NET.Sdk.Worker (built-in)**: Worker host type — `IHostedService` / `BackgroundService` infrastructure ships with .NET SDK, no NuGet package
- **System.Threading.PeriodicTimer (built-in)**: Reconciliation scheduling — cleaner than `Task.Delay` loops; fires on schedule, skips missed ticks
- **EF Core RowVersion (existing)**: Optimistic concurrency token — already on `Ticket` entity, requires only `.IsRowVersion()` in EF configuration to activate
- **rabbitmq:3-management Docker image**: Local broker — includes management UI on port 15672 for debugging; conservative and stable version choice

### Expected Features

**Must have (table stakes):**
- Publish `TicketCreatedEvent` from `CreateTicketCommandHandler` after `SaveChangesAsync` — the pipeline entry point
- `Ticketing.Worker` project consuming the `ticket-assignment` queue via MassTransit — core delivery mechanism
- `AgentProfile` EF Core configuration, migration, and seed data — algorithm has no inputs without this
- `AutoAssignTicketCommandHandler` with weighted ranking algorithm — the assignment decision logic
- `TicketAssignmentHistory` persistence for every auto-assignment — audit trail and operational visibility
- Concurrency protection: catch `DbUpdateConcurrencyException`, treat already-assigned ticket as success
- Reconciliation `BackgroundService` with age threshold query — safety net for lost events
- Worker service in `docker-compose.yml` with RabbitMQ health-check dependency

**Should have (differentiators):**
- Agent profile management API (Admin-only CRUD) — lets admins tune capacity without a DB migration
- Agent load visibility endpoint (`GET /api/agents/load`) — operational dashboard for admins
- Assignment history endpoint (`GET /api/tickets/{id}/assignments`) — audit access for agents and admins
- Worker health check endpoint — enables Docker and orchestrators to detect a stuck consumer

**Defer (v2+):**
- Notification system (email/SMS/push) on assignment — separate bounded concern
- Real-time WebSocket push — high infrastructure cost for low milestone value
- Skill-based routing — requires skills taxonomy and categorization, a separate feature
- Dead-letter queue automated handling — reconciliation worker covers the same failure class more simply

### Architecture Approach

The system uses a strict layered dependency model: `Ticketing.Worker` and `Ticketing.API` are both deployable hosts that reference `Ticketing.Application` and `Ticketing.Infrastructure` but never each other. The worker registers only persistence-related services (`AddPersistence`), not JWT auth or HTTP middleware. The `IMessagePublisher` interface lives in `Ticketing.Application` so the Application layer has no RabbitMQ dependency — Infrastructure implements it. All assignment business logic lives in `AutoAssignTicketCommandHandler` so it is reachable from both the event consumer and the reconciliation sweep without duplication.

**Major components:**
1. **Ticketing.API (modified)** — publishes `TicketCreatedEvent` after ticket save; no assignment logic; returns 201 immediately
2. **RabbitMQ** — exchange `ticketing.events`, queue `ticket-assignment`; decouples creation from assignment latency
3. **Ticketing.Worker** — `TicketAssignmentConsumer` translates AMQP messages to `AutoAssignTicketCommand`; `ReconciliationBackgroundService` sweeps for missed tickets
4. **AutoAssignTicketCommandHandler** — weighted ranking algorithm (projected load → idle time → efficiency → id); idempotency check at top; concurrency protection at save
5. **AgentProfile + TicketAssignmentHistory** — data model backing the algorithm and audit trail
6. **Ticketing.Infrastructure (extended)** — `RabbitMqPublisher`, new EF configs, new repositories, split DI registration

### Critical Pitfalls

1. **Scoped services in singleton BackgroundService** — inject `IServiceScopeFactory`, create an explicit scope per message; `DbContext` is not thread-safe as a singleton and will corrupt the change tracker silently
2. **RowVersion not configured in EF** — `Ticket.RowVersion` exists as a property but `TicketConfiguration` lacks `.IsRowVersion()`; without this, concurrent assignments are never caught and `DbUpdateConcurrencyException` is never thrown
3. **Wrong acknowledgment order** — set `autoAck: false`; ack only after `SaveChangesAsync` succeeds; nack with requeue for transient errors, nack without requeue for permanent failures (empty agent pool, ticket not found)
4. **DI registration breakdown in worker** — `AddInfrastructure()` currently bundles JWT and `IHttpContextAccessor` which have no meaning in the worker; split into `AddPersistence()` and `AddApiInfrastructure()` before wiring the worker
5. **Agent load count including closed tickets** — the load query must filter to active statuses only (`Open`, `Assigned`, `InProgress`); counting resolved/closed tickets permanently skews the algorithm toward newer agents

---

## Implications for Roadmap

Based on the dependency graph across all research files, the build must proceed in strict data-before-logic order. There is no phase where messaging can be built before the data model, and no phase where the worker can be built before the Application handler.

### Phase 1: Domain and Persistence Foundation

**Rationale:** Every other phase depends on `AgentProfile`, `TicketAssignmentHistory`, and their repository interfaces. The EF configuration (including `.IsRowVersion()` on `Ticket`) and migration must exist before any handler or consumer code is written — otherwise concurrency protection is built on a silent no-op.
**Delivers:** `AgentProfile` entity, `TicketAssignmentHistory` entity, `AssignmentType` enum, `IAgentProfileRepository`, `IAssignmentHistoryRepository`, EF Core configurations, migration with correct `rowversion` type, database seeder for agent profiles
**Addresses:** FEATURES.md — AgentProfile persistence, TicketAssignmentHistory persistence
**Avoids:** Pitfall 6 (RowVersion not configured), Pitfall 13 (enum shift corrupting existing data), Pitfall 4 (load count built on wrong data model)

### Phase 2: Application Layer — Command and Algorithm

**Rationale:** The `AutoAssignTicketCommand`, its handler, and the `IMessagePublisher` interface must exist before both the API publisher and the worker consumer can be built. The ranking algorithm belongs here — not in infrastructure — so it is testable and reachable from both the event path and the reconciliation path.
**Delivers:** `IMessagePublisher` interface, `AutoAssignTicketCommand`, `AutoAssignTicketCommandHandler` with weighted ranking algorithm, `GetAvailableAgentsWithLoadAsync` repository method, `AgentProfile.RecordAssignment(DateTime)` domain behavior
**Uses:** EF Core repositories from Phase 1, MediatR (existing), RowVersion concurrency from Phase 1
**Avoids:** Pitfall 4 (load count filtered to active statuses only), Pitfall 10 (DateTime passed as parameter, not `DateTime.UtcNow` in entity)

### Phase 3: Event Publishing in the API

**Rationale:** Modifying `CreateTicketCommandHandler` to publish `TicketCreatedEvent` requires the `IMessagePublisher` interface (Phase 2) and the `RabbitMqPublisher` implementation. This is a single touch point in existing code — isolated change with no breaking surface.
**Delivers:** `RabbitMqPublisher : IMessagePublisher` in Infrastructure, `CreateTicketCommandHandler` modified to publish after `SaveChangesAsync`, `MassTransit.RabbitMQ` added to API project
**Uses:** MassTransit.RabbitMQ 8.x
**Avoids:** Pitfall 12 (publish strictly after commit, not before)

### Phase 4: Worker Service and Reconciliation

**Rationale:** The worker can only be built after the handler (Phase 2) and publisher infrastructure (Phase 3) are in place. This phase creates the `Ticketing.Worker` project, wires it with only persistence DI, and implements both the event consumer and the reconciliation sweep.
**Delivers:** `Ticketing.Worker` project, `TicketAssignmentConsumer : BackgroundService` (MassTransit consumer), `ReconciliationBackgroundService` with `PeriodicTimer`, split `DependencyInjection.cs` (`AddPersistence` vs `AddApiInfrastructure`)
**Implements:** Worker host, AMQP consume loop, idempotency check, age-threshold reconciliation query
**Avoids:** Pitfall 3 (IServiceScopeFactory per message), Pitfall 5 (DI registration split), Pitfall 2 (manual ack after commit), Pitfall 8 (empty agent pool nacked without requeue), Pitfall 9 (reconciliation age threshold prevents assignment storms)

### Phase 5: Docker Integration

**Rationale:** Docker wiring depends on the worker project existing and RabbitMQ being referenced. Adding healthchecks and service dependencies is a small, discrete step with no application logic.
**Delivers:** RabbitMQ service in `docker-compose.yml` with `rabbitmq-diagnostics` healthcheck, Worker service with `depends_on: rabbitmq: condition: service_healthy`, updated `.dockerignore` and `Dockerfile` for worker project
**Avoids:** Pitfall 11 (worker starts before broker is ready)

### Phase 6: Admin and Operational APIs

**Rationale:** Agent profile management and load visibility endpoints are differentiators that require all prior phases to be functional. They add operational control without touching the core assignment pipeline.
**Delivers:** Agent profile CRUD endpoints (Admin-only), `GET /api/agents/load`, `GET /api/tickets/{id}/assignments`, worker health check endpoint
**Addresses:** FEATURES.md differentiator features

### Phase Ordering Rationale

- Domain and persistence first because the algorithm, handler, and all consumers require entity types and query methods to exist
- Application handler before worker because the consumer is a thin translator — it should not contain business logic
- API publisher before worker because the worker tests the full end-to-end path and requires something on the publish side
- Docker last (within core phases) because it adds no logic; it wires what already exists
- Admin APIs last because they are additive and do not block any other phase

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 4 (Worker + MassTransit):** MassTransit consumer API details and exact configuration overloads for receive endpoint should be verified against installed 8.x version; training data has a cutoff and patch-version behavior may differ
- **Phase 4 (Reconciliation design):** Edge cases around storm prevention (Pitfall 9) need explicit test scenarios designed before implementation

Phases with standard patterns (can proceed without additional research):
- **Phase 1 (Domain + Persistence):** EF Core configuration, migrations, and concurrency tokens are well-documented, stable APIs
- **Phase 2 (Application handler):** LINQ ranking and MediatR handler patterns are established; no external integration
- **Phase 5 (Docker):** Docker Compose healthcheck pattern is already used for SQL Server in the existing `docker-compose.yml`; same pattern applied to RabbitMQ

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | MEDIUM | .NET 10 and existing packages confirmed from csproj; MassTransit 8.x version is training-data derived — verify current patch on NuGet before pinning |
| Features | HIGH | Based on direct codebase analysis plus well-understood assignment system patterns; feature boundaries are clear |
| Architecture | HIGH | Based on direct codebase analysis; component boundaries and data flow patterns are well-established .NET idioms |
| Pitfalls | MEDIUM-HIGH | Critical pitfalls (DI scoping, RowVersion config, ack order) are HIGH confidence based on stable .NET and EF Core behavior; RabbitMQ client-specific ack API details are MEDIUM |

**Overall confidence:** MEDIUM-HIGH

### Gaps to Address

- **MassTransit exact version**: Training data shows 8.x as current stable as of August 2025. Verify the latest stable 8.x patch on https://www.nuget.org/packages/MassTransit.RabbitMQ before adding the package reference. Pin both the API and Worker to the same version.
- **RabbitMQ 4.x stability**: The `rabbitmq:3-management` Docker image tag is the safe conservative choice. If RabbitMQ 4 has reached stable status, `4-management` may be preferable. Verify before committing the Docker Compose service definition.
- **Existing `AssignTicketCommandHandler` gap**: Research notes that manual assignment (`PUT /api/tickets/{id}/assign`) does not currently write a `TicketAssignmentHistory` record. This should be addressed in Phase 1 or Phase 2 to keep the audit trail consistent between manual and auto-assignment paths.
- **`TicketStatus` enum migration**: The existing `Assigned = 1` value in `TicketStatus` may have shifted prior enum integer mappings. Before running any new migration, inspect existing data and determine whether a data migration script is required. Document the decision in `PROBLEM_LOG.md`.

---

## Sources

### Primary (HIGH confidence)
- Existing csproj files (`Ticketing.Infrastructure`, `Ticketing.API`, `Ticketing.Application`) — version baseline
- Existing domain entities (`Ticket.cs`, `AgentProfile.cs`, `TicketAssignmentHistory.cs`) — data model
- `.planning/PROJECT.md` — milestone scope and constraints
- `.planning/codebase/ARCHITECTURE.md` — existing architecture
- .NET BackgroundService + IServiceScopeFactory DI scoping — stable .NET pattern
- EF Core `IsRowVersion()` concurrency token — stable EF Core API

### Secondary (MEDIUM confidence)
- MassTransit 8.x documentation (training data, August 2025 cutoff) — consumer and topology configuration
- RabbitMQ AMQP acknowledgment model — ack/nack/requeue behavior
- Ticket assignment system patterns (Zendesk, Jira Service Management, Freshdesk) — algorithm design validation

### Tertiary (LOW confidence)
- RabbitMQ 4.x release status — unknown; use `3-management` tag until verified

---

*Research completed: 2026-03-13*
*Ready for roadmap: yes*
