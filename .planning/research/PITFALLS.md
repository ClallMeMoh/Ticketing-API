# Domain Pitfalls

**Domain:** .NET Worker Service + RabbitMQ + Auto-Assignment Algorithm
**Project:** Ticket Management System — Auto-Assignment Extension
**Researched:** 2026-03-13
**Confidence:** MEDIUM-HIGH (training data + codebase analysis; no live web search available)

---

## Critical Pitfalls

Mistakes that cause data corruption, double-assignment, or require rewrites.

---

### Pitfall 1: Double-Assignment from Concurrent Message Processing

**What goes wrong:** Two worker instances (or two rapid events) both read the same ticket as unassigned, independently select the best agent, and both commit an assignment. The ticket ends up assigned twice — the second write silently overwrites the first, corrupting the history and agent load counts.

**Why it happens:** A naive "read ticket → pick agent → save" flow has a race window between the read and the write. RabbitMQ can deliver the same message to multiple consumers if prefetch is misconfigured, or the reconciliation worker fires while the event consumer is mid-flight.

**Consequences:** One assignment is silently lost. AgentProfile load counts diverge from reality. TicketAssignmentHistory is missing entries. The ticket may bounce between agents on reconciliation sweeps.

**Prevention:**
- The `RowVersion` concurrency token is already on `Ticket`. Use it. Wrap the assignment save in a try/catch for `DbUpdateConcurrencyException`. On conflict, reload the ticket — if it is already assigned, discard the message and ack it; this is correct behavior, not an error.
- Set RabbitMQ channel `prefetchCount = 1` so a single consumer processes one message at a time before accepting the next.
- In the reconciliation worker, add a `WHERE Status = 'Open' AND AssignedToUserId IS NULL` filter and use the same optimistic concurrency pattern.

**Warning signs:**
- TicketAssignmentHistory has duplicate rows for the same TicketId with the same AssignedAt timestamp.
- AgentProfile.LastAssignedAt shows a time after the ticket was already resolved.
- Unit tests for the assignment handler start flaking under concurrent execution.

**Phase:** Address in the worker service implementation phase, before any reconciliation worker work.

---

### Pitfall 2: Message Acknowledgment at the Wrong Point

**What goes wrong:** The worker sends a RabbitMQ `basicAck` immediately when it receives the message, before the database transaction commits. If the assignment fails (DB error, exception, process crash), the message is gone — the ticket stays unassigned silently.

**The opposite mistake is also dangerous:** Auto-ack is enabled on the channel, so every received message is acked automatically. Any handler exception means the event is permanently lost.

**Why it happens:** RabbitMQ client libraries default to auto-ack in some configurations. When developers copy quickstart examples, they often use `autoAck: true` without understanding the implication.

**Consequences:** Tickets remain Open with no assignment and no retry. The reconciliation worker is the only safety net — but if it is not built yet (or has a bug), tickets accumulate unassigned indefinitely.

**Prevention:**
- Set `autoAck: false` on the channel.
- Send `BasicAck` only after `SaveChangesAsync` returns successfully.
- On exception, send `BasicNack` with `requeue: true` for transient errors (DB connectivity). For permanent failures (ticket not found, agent pool empty), send `BasicNack` with `requeue: false` and log — do not requeue infinitely.
- Add a dead-letter exchange so permanently rejected messages land somewhere visible, not silently dropped.

**Warning signs:**
- Tickets created during a DB restart remain Open permanently.
- RabbitMQ management UI shows messages being delivered but the queue depth never grows (all auto-acked).
- Assignment history has gaps that correlate with process restarts.

**Phase:** Address in the RabbitMQ setup phase, before any handler logic is written. This is a channel-level configuration decision.

---

### Pitfall 3: Scoped Services Injected Into the Singleton Worker

**What goes wrong:** `BackgroundService` (and `IHostedService`) implementations are registered as singletons by the .NET hosting model. If `ApplicationDbContext`, `ITicketRepository`, or `IAgentProfileRepository` are injected directly into the worker constructor, they are captured as singletons for the worker's lifetime. EF Core `DbContext` is not thread-safe as a singleton. Data from the first request is cached indefinitely; concurrent calls corrupt the change tracker.

**Why it happens:** This is the most common .NET DI mistake with hosted services. The container resolves the dependency at registration time (singleton scope), and no DI warning is raised at startup by default in .NET 10.

**Consequences:** Stale entity reads from the cached context. `DbUpdateConcurrencyException` from the change tracker holding stale RowVersion values. Random `ObjectDisposedException` after the first scope is disposed.

**Prevention:**
- Inject `IServiceScopeFactory` into the worker constructor (it is singleton-safe).
- Inside each message handler invocation, create an explicit scope: `using var scope = _scopeFactory.CreateScope()`.
- Resolve `ITicketRepository`, `IAgentProfileRepository`, and `IUnitOfWork` from that scope.
- Dispose the scope when the message handling is complete.

**Warning signs:**
- Assignment works correctly for the first message after startup, then starts failing or returning stale data for subsequent messages.
- The `DbContext` change tracker shows entities in unexpected states.
- `InvalidOperationException: A second operation was started on this context instance` in logs.

**Phase:** Address during the worker project scaffolding phase, before any domain logic is written into the worker.

---

### Pitfall 4: Agent Load Count Computed Incorrectly

**What goes wrong:** The auto-assignment algorithm uses a "projected load" to rank agents — typically counting active tickets assigned to each agent. If the query counts all ticket statuses (including Closed and Resolved), agents who resolved many tickets appear heavily loaded, and the algorithm always routes to newer agents. The load distribution becomes permanently skewed.

**Why it happens:** The load query is written as `COUNT(AssignedToUserId)` without a `WHERE Status IN ('Assigned', 'InProgress')` filter. It is easy to miss because the behavior is not obviously wrong — assignments still happen, just with poor distribution.

**Consequences:** One or two agents receive no new tickets while others are overloaded. The efficiency score becomes meaningless because it is compared against inflated projected loads. The algorithm degrades to pseudo-random assignment over time.

**Prevention:**
- The load count query must explicitly filter to active statuses only: `Open`, `Assigned`, `InProgress`.
- Define this set as a named constant or static readonly set in the domain or application layer — do not scatter magic status lists.
- Write a dedicated repository method `GetAvailableAgentsWithLoadAsync()` that encapsulates the join and filter. Do not build this inline in the handler.
- Add a unit test that creates agents with a mix of closed and active tickets and asserts the load counts match only the active ones.

**Warning signs:**
- Agents with high historical ticket counts are never selected despite having no active tickets.
- The algorithm always picks the same recently-created agent.
- Load counts in the admin API do not decrease when tickets are closed.

**Phase:** Address during algorithm design, before writing any ranking logic.

---

### Pitfall 5: Worker Project Breaking the Existing DI and EF Migrations Setup

**What goes wrong:** The worker is a separate `Worker` project that references `Ticketing.Infrastructure`. When the worker registers `AddInfrastructure(...)`, it triggers EF Core migration checks or seeding logic that expects the full API environment (JWT settings, `IHttpContextAccessor`, etc.). The worker fails to start because `ICurrentUserService` depends on `IHttpContextAccessor`, which is not available in the worker host.

**Why it happens:** `DependencyInjection.cs` in Infrastructure registers everything as a bundle: `IHttpContextAccessor`, `ICurrentUserService`, JWT bearer authentication. The worker only needs the DB context and repositories — not auth middleware — but it gets the whole bundle.

**Consequences:** Worker fails at startup with a DI resolution error or `NullReferenceException` in `CurrentUserService.UserId`. Running EF migrations from the worker project fails because the design-time factory or startup project is ambiguous.

**Prevention:**
- Split `DependencyInjection.cs` into two extension methods: `AddPersistence(...)` (DbContext, repositories, TimeProvider) and `AddApiInfrastructure(...)` (JWT, `ICurrentUserService`, `IHttpContextAccessor`).
- The worker calls only `AddPersistence(...)`. The API calls both.
- Keep EF migrations in the Infrastructure project. Specify `--startup-project` explicitly when running `dotnet ef` from the solution root to avoid ambiguity.
- The worker should never resolve `ICurrentUserService`. Assignment logic uses the agent ID directly from the event payload or the algorithm output.

**Warning signs:**
- Worker project builds but crashes at startup with `InvalidOperationException` related to `IHttpContextAccessor`.
- `dotnet ef migrations add` picks the wrong startup project and fails with missing configuration.
- DatabaseSeeder (which seeds the admin user) runs in the worker on startup, causing duplicate-key errors if the API is also running.

**Phase:** Address during the worker project scaffolding phase, immediately after creating the project.

---

### Pitfall 6: The `RowVersion` Column Missing from EF Configuration for New Tables

**What goes wrong:** `Ticket` has a `RowVersion` property for optimistic concurrency, but the EF configuration (`TicketConfiguration`) does not yet declare it as a concurrency token with `.IsRowVersion()`. Without this, EF Core does not include the column in the `WHERE` clause of UPDATE statements. The concurrency protection is silently disabled even though the column exists in the schema.

**Why it happens:** The `Ticket` entity already has `public byte[] RowVersion { get; private set; }` (confirmed in codebase), but looking at `TicketConfiguration.cs`, there is no `.Property(t => t.RowVersion).IsRowVersion()` configuration. EF Core requires explicit configuration for concurrency tokens unless convention-based detection is enabled.

**Consequences:** `DbUpdateConcurrencyException` is never thrown on concurrent updates. The double-assignment race condition (Pitfall 1) has no database-level protection even though the property exists. The entire optimistic concurrency strategy is a no-op.

**Prevention:**
- Add `.Property(t => t.RowVersion).IsRowVersion()` to `TicketConfiguration.Configure()`.
- Verify by writing a test that simultaneously loads the same ticket in two contexts, updates both, and confirms the second save throws `DbUpdateConcurrencyException`.
- When adding the migration for new tables (AgentProfile, TicketAssignmentHistory), verify the generated migration includes the `rowversion` column type for `Ticket`.

**Warning signs:**
- `DbUpdateConcurrencyException` is never raised in tests even when it should be.
- The generated EF migration SQL does not include `rowversion` as the column type for `Ticket.RowVersion`.
- SQL Server profiler shows UPDATE statements without a `WHERE RowVersion = @p0` clause.

**Phase:** Address in the EF Core migration phase, as the first step before writing any concurrency-dependent code.

---

## Moderate Pitfalls

### Pitfall 7: RabbitMQ Connection Not Resilient to Broker Restart

**What goes wrong:** The worker opens a single `IConnection` to RabbitMQ at startup. If RabbitMQ restarts (e.g., during `docker compose restart rabbitmq`), the TCP connection drops. The worker does not reconnect and silently stops consuming messages. No exception is surfaced in logs because the error occurs on the channel level, not in application code.

**Prevention:**
- Use a connection retry policy at startup (e.g., Polly retry with exponential backoff) to handle the case where RabbitMQ is not yet ready when the worker starts.
- Subscribe to `IConnection.ConnectionShutdown` and `IModel.ModelShutdown` events to detect drops.
- On a dropped connection, restart the consumer via a retry loop or by restarting the hosted service.
- Alternatively, use MassTransit or a similar abstraction that handles reconnection internally — though for this project's scope, a manual retry is simpler and more transparent.

**Warning signs:**
- Worker logs show "Connected to RabbitMQ" at startup but no messages processed after a broker restart.
- Queue depth grows in the RabbitMQ management UI but no consumers are listed.

**Phase:** Address during RabbitMQ integration phase, as part of the initial channel setup.

---

### Pitfall 8: Agent Pool Can Be Empty at Assignment Time

**What goes wrong:** The assignment algorithm selects the best agent from `GetAvailableAgentsWithLoadAsync()`. If no agents are available (all marked unavailable, or no `AgentProfile` rows exist), the algorithm panics with a null reference or throws an unhandled exception that causes the message to be nacked and requeued in a tight loop.

**Prevention:**
- Explicitly check for an empty agent pool after querying. If the pool is empty, nack the message with `requeue: false` and log a warning — do not requeue, as the condition will not resolve by retrying immediately.
- The reconciliation worker will pick up unassigned tickets when agents come back online.
- Ensure the `DatabaseSeeder` or setup documentation creates at least one agent `AgentProfile` for development and testing environments.

**Warning signs:**
- RabbitMQ queue depth grows rapidly without shrinking.
- Worker logs show repeated processing of the same message ID.
- CPU spikes from a tight nack/requeue/redeliver loop.

**Phase:** Address in the algorithm implementation phase.

---

### Pitfall 9: Reconciliation Worker and Event Consumer Creating Assignment Storms

**What goes wrong:** The reconciliation worker sweeps for unassigned tickets and queues them for assignment. If the sweep interval is too short (e.g., every 5 seconds), and the event consumer is slow (processing takes 3 seconds per ticket), the reconciliation worker will continuously re-enqueue the same tickets that are already mid-flight. This causes multiple simultaneous assignments for the same ticket.

**Prevention:**
- The reconciliation worker should query for tickets where `Status = Open AND AssignedToUserId IS NULL AND CreatedAt < NOW() - [threshold]` — add a threshold (e.g., 2 minutes) to avoid re-queuing tickets that were just created and are already in the primary queue.
- Use the same optimistic concurrency protection (RowVersion) in reconciliation assignment, so the second attempt fails gracefully.
- Set the reconciliation interval to something sensible (5-10 minutes) — it is a safety net, not a primary driver.

**Warning signs:**
- TicketAssignmentHistory shows more than one Auto assignment for the same ticket within seconds.
- Agent load counts are higher than expected relative to the actual ticket count.

**Phase:** Address when designing the reconciliation worker, before implementing it.

---

### Pitfall 10: `AgentProfile.RecordAssignment()` Using `DateTime.UtcNow` Directly

**What goes wrong:** `AgentProfile.RecordAssignment()` sets `LastAssignedAt = DateTime.UtcNow` directly in the entity. This is inconsistent with the established `TimeProvider` pattern used in `ApplicationDbContext.SetAuditFields()`. In tests, time cannot be controlled, making ordering assertions unreliable.

**Why it happens:** The domain entity was written without awareness of the `TimeProvider` convention established in the infrastructure layer.

**Prevention:**
- Change `RecordAssignment()` to accept a `DateTime assignedAt` parameter: `public void RecordAssignment(DateTime assignedAt)`. The caller (the worker handler) passes `_timeProvider.GetUtcNow().UtcDateTime`.
- This keeps the domain entity pure (no infrastructure dependency) while maintaining testability.
- Consistent with the pattern already established in `ApplicationDbContext`.

**Warning signs:**
- Unit tests for the assignment algorithm cannot control `LastAssignedAt` for tie-breaking scenarios.
- `AgentProfile` tests set `LastAssignedAt` via reflection or other workarounds.

**Phase:** Address during domain extension phase, when implementing AgentProfile behavior.

---

## Minor Pitfalls

### Pitfall 11: Docker Compose Service Startup Order

**What goes wrong:** The worker starts before RabbitMQ is ready. The `depends_on` directive only waits for the container to start, not for RabbitMQ to finish its boot sequence and open the AMQP port. The worker's connection attempt fails, and if there is no retry logic, the worker exits.

**Prevention:**
- Add a `healthcheck` to the RabbitMQ service in `docker-compose.yml` using `rabbitmq-diagnostics -q ping`.
- Set `depends_on: rabbitmq: condition: service_healthy` for both the API and Worker services.
- This matches the existing pattern already used for `sqlserver` in the current `docker-compose.yml`.

**Warning signs:**
- Worker container exits with code 1 shortly after starting.
- Logs show "Connection refused" to the RabbitMQ port.

**Phase:** Address in the Docker integration phase.

---

### Pitfall 12: Ticket Event Published Before Transaction Commits

**What goes wrong:** `CreateTicketCommandHandler` saves the ticket to the database and then publishes a `TicketCreatedEvent` to RabbitMQ. If the publish happens before `SaveChangesAsync` completes, or if `SaveChangesAsync` is called inside the same scope as the publish and rolls back, the worker receives an event for a ticket that does not exist in the database. The assignment query returns null and fails.

**Prevention:**
- Always call `SaveChangesAsync` before publishing the event. The event is a side effect of a successful write, not part of the same transaction.
- The outbox pattern (write event to DB table in same transaction, then relay to RabbitMQ) is the fully reliable solution, but it is out of scope here. For this system, "save first, then publish" is sufficient.
- The reconciliation worker acts as the safety net for messages lost between the two steps.

**Warning signs:**
- Worker logs show "Ticket not found" errors immediately after a new event is received.
- The error correlates with high load (more likely under concurrent ticket creation).

**Phase:** Address when implementing `CreateTicketCommandHandler` event publishing.

---

### Pitfall 13: EF Migration for New Tables Breaks Existing Enum Values

**What goes wrong:** `TicketStatus` has a new `Assigned = 1` value inserted between existing values, shifting `InProgress` from 1 to 2, `Resolved` from 2 to 3, `Closed` from 3 to 4. If existing production data was written with the old numeric mapping, all existing tickets with `Status = 1` (previously InProgress) are now read as `Assigned`. This is a data corruption issue.

**Why it happens:** Inserting a new enum value in the middle of the sequence changes the integer mapping of all subsequent values. The current codebase shows `Assigned = 1` already exists in `TicketStatus`, but the migration has not been run yet.

**Prevention:**
- The migration must include a `UPDATE Tickets SET Status = Status + 1 WHERE Status >= 1` data migration script to shift existing rows.
- Alternatively, use an explicit `ALTER TABLE` with a default to handle the transition.
- Since this is a development project (not production), a clean database reset via `DROP DATABASE` and fresh migration is acceptable — but document it in PROBLEM_LOG.md.
- For production systems: always insert new enum values at the end, or include a data migration in the EF migration file.

**Warning signs:**
- Existing tickets that were `InProgress` before the migration appear as `Assigned` after.
- The number of `Assigned` tickets is suspiciously large after migration.

**Phase:** Address as the very first step of the EF migration phase, before running any migration.

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Worker project scaffolding | Scoped services captured as singletons (Pitfall 3) | Use `IServiceScopeFactory`, create explicit scope per message |
| Worker project scaffolding | DI registration breakdown from API-specific services (Pitfall 5) | Split `DependencyInjection.cs` into persistence-only and API-specific parts |
| EF Core migration | RowVersion not configured as concurrency token (Pitfall 6) | Add `.IsRowVersion()` to TicketConfiguration before writing any migration |
| EF Core migration | Enum value shift corrupting existing data (Pitfall 13) | Include data migration script or reset DB; document in PROBLEM_LOG.md |
| RabbitMQ setup | Auto-ack silently dropping failed messages (Pitfall 2) | Set `autoAck: false`; ack only after successful DB commit |
| RabbitMQ setup | No reconnection on broker restart (Pitfall 7) | Add startup retry policy and channel shutdown event handler |
| Algorithm design | Load count includes resolved/closed tickets (Pitfall 4) | Filter to active statuses only; define status set as named constant |
| Algorithm design | Empty agent pool causing infinite requeue loop (Pitfall 8) | Explicit empty-pool check; nack with `requeue: false` if pool is empty |
| Assignment write path | Double-assignment race condition (Pitfall 1) | Catch `DbUpdateConcurrencyException`; treat already-assigned as success |
| AgentProfile domain extension | `DateTime.UtcNow` inconsistency (Pitfall 10) | Pass `DateTime` as parameter to `RecordAssignment()` |
| Reconciliation worker | Assignment storms from too-frequent sweeps (Pitfall 9) | Add age threshold to query; use 5-10 minute interval |
| Event publishing | Ticket event published before transaction commits (Pitfall 12) | Publish after `SaveChangesAsync` returns; reconciliation is the safety net |
| Docker integration | Worker starts before RabbitMQ is ready (Pitfall 11) | Add RabbitMQ healthcheck; use `condition: service_healthy` |

---

## Sources

- Codebase analysis: existing `Ticket.cs`, `AgentProfile.cs`, `ApplicationDbContext.cs`, `TicketConfiguration.cs`, `DependencyInjection.cs`, `AssignTicketCommandHandler.cs` (HIGH confidence — direct inspection)
- PROBLEM_LOG.md: established patterns (TimeProvider, DI split concerns, EF Core conventions) (HIGH confidence — project history)
- .planning/PROJECT.md: milestone requirements and constraints (HIGH confidence — project spec)
- .NET BackgroundService + DI scoping behavior: training data, well-documented pattern (HIGH confidence)
- RabbitMQ AMQP acknowledgment model: training data on RabbitMQ.Client library (MEDIUM confidence — verify prefetch and ack API against current RabbitMQ.Client version)
- EF Core optimistic concurrency via `IsRowVersion()`: training data (HIGH confidence — stable API)
- Enum-shifting migration risk: training data + codebase inspection of TicketStatus values (HIGH confidence)
