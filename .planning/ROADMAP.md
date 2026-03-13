# Roadmap: Ticket Auto-Assignment Extension

## Overview

This milestone extends the existing Ticket Management System with an asynchronous auto-assignment pipeline. A new `Ticketing.Worker` service consumes `TicketCreatedEvent` messages from RabbitMQ and runs a weighted load-balancing algorithm to assign each ticket to the best available agent. The build proceeds in strict data-before-logic order: domain entities and persistence first, the assignment algorithm second, event publishing third, the worker consumer fourth, Docker wiring fifth, and the admin operational APIs last.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Domain and Persistence Foundation** - Extend the domain with AgentProfile, TicketAssignmentHistory, the new Assigned status, and the EF migration
- [ ] **Phase 2: Assignment Algorithm** - Implement AutoAssignTicketCommand and the weighted ranking handler in the Application layer
- [ ] **Phase 3: Event Publishing** - Publish TicketCreatedEvent from CreateTicketCommandHandler after successful database commit
- [ ] **Phase 4: Worker Service** - Create Ticketing.Worker, wire the MassTransit consumer and reconciliation sweep
- [ ] **Phase 5: Docker Integration** - Add RabbitMQ and Worker to docker-compose with health checks and correct startup ordering
- [ ] **Phase 6: Admin and Operational APIs** - Agent profile CRUD and load visibility endpoints for Admin users

## Phase Details

### Phase 1: Domain and Persistence Foundation
**Goal**: The data model backing the entire auto-assignment pipeline exists, is correctly configured in EF Core, and the database migration runs cleanly against existing data
**Depends on**: Nothing (existing codebase)
**Requirements**: DOM-01, DOM-02, DOM-03, DOM-04, DOM-05, DOM-06, DOM-07
**Success Criteria** (what must be TRUE):
  1. Ticket status enum includes `Assigned` between `Open` and `InProgress`, and existing tickets in the database retain their correct status after migration
  2. `Ticket.AssignTo()` sets status to `Assigned` and the status transition rules (Assigned → InProgress, Assigned → Open, Closed cannot go to Assigned) are enforced by the domain
  3. `AgentProfile` and `TicketAssignmentHistory` entities exist with all required fields and are persisted via EF Core with correct table configurations
  4. `Ticket.RowVersion` is configured with `.IsRowVersion()` in EF Core so that concurrent saves to the same ticket throw `DbUpdateConcurrencyException`
  5. EF migration applies successfully to a fresh and an existing database without data loss
**Plans**: 2 plans

Plans:
- [x] 01-01-PLAN.md — Domain fixes, EF configurations, DbContext updates, and domain unit tests
- [ ] 01-02-PLAN.md — Repository implementations, handler history wiring, seeder extension, and EF migration

### Phase 2: Assignment Algorithm
**Goal**: The weighted load-balancing algorithm exists in the Application layer, is reachable from both the event consumer and the reconciliation sweep, and executes in two queries
**Depends on**: Phase 1
**Requirements**: ALG-01, ALG-02, ALG-03, ALG-04, ALG-05, ALG-06, ALG-07
**Success Criteria** (what must be TRUE):
  1. `AutoAssignTicketCommandHandler` selects the agent with the lowest projected load ratio (using Low=1, Medium=2, High=3, Critical=5 weights) and assigns the ticket, writing a `TicketAssignmentHistory` record with type Auto and a structured reason string
  2. When no eligible agent exists (none available or all at capacity), the ticket remains Open and a warning is logged — no exception is thrown
  3. Concurrent dispatch of `AutoAssignTicketCommand` for the same ticket is resolved by catching `DbUpdateConcurrencyException`; the second invocation exits cleanly (idempotency check at entry)
  4. The handler executes using at most two database queries — one to fetch available agents with their active ticket loads, one to persist the assignment
  5. Agent load count only includes tickets in Open, Assigned, or InProgress status — resolved and closed tickets do not skew the ranking
**Plans**: TBD

### Phase 3: Event Publishing
**Goal**: Every successfully created ticket publishes a `TicketCreatedEvent` to the RabbitMQ exchange after the database commit completes
**Depends on**: Phase 2
**Requirements**: MSG-01, MSG-02
**Success Criteria** (what must be TRUE):
  1. Creating a ticket via `POST /api/tickets` results in a message appearing in the `ticket-assignment` queue on the `ticketing.events` exchange with routing key `ticket.created`
  2. The `TicketCreatedEvent` is published strictly after `SaveChangesAsync` succeeds — a database failure does not produce a phantom event
  3. `IMessagePublisher` lives in the Application layer with no RabbitMQ dependency; `RabbitMqPublisher` in Infrastructure implements it
**Plans**: TBD

### Phase 4: Worker Service
**Goal**: The `Ticketing.Worker` process runs independently, consumes assignment events from RabbitMQ, dispatches the assignment handler, and reconciles any tickets missed during broker downtime
**Depends on**: Phase 3
**Requirements**: MSG-03, MSG-04, MSG-05, WRK-01, WRK-02, WRK-03, WRK-04, WRK-05
**Success Criteria** (what must be TRUE):
  1. A ticket created via the API is automatically assigned to an available agent within seconds, and a `TicketAssignmentHistory` record with type Auto appears in the database
  2. When RabbitMQ is temporarily unavailable, the worker retries the connection with exponential backoff and resumes consuming without a process restart
  3. A message is acknowledged only after `SaveChangesAsync` succeeds; a transient failure nacks with requeue, a permanent failure (no agents, ticket not found) nacks without requeue
  4. The reconciliation sweep runs every 60 seconds and assigns any Open tickets older than the configured age threshold that were not assigned through the event path
  5. Worker failures are logged with structured logging and do not crash the service process
**Plans**: TBD

### Phase 5: Docker Integration
**Goal**: A single `docker-compose up --build` starts API, Worker, SQL Server, and RabbitMQ together with correct startup dependencies and health checks
**Depends on**: Phase 4
**Requirements**: DOC-01, DOC-02, DOC-03, DOC-04
**Success Criteria** (what must be TRUE):
  1. `docker-compose up --build` starts all four services (API, Worker, SQL Server, RabbitMQ) with no manual intervention
  2. The Worker service does not start consuming until the RabbitMQ health check passes, preventing startup-time connection errors
  3. The RabbitMQ management UI is accessible at `http://localhost:15672` after compose starts
  4. The Worker Dockerfile builds successfully and produces a working container image
**Plans**: TBD

### Phase 6: Admin and Operational APIs
**Goal**: Administrators can manage agent profiles and observe current agent load through the API without direct database access
**Depends on**: Phase 4
**Requirements**: AGT-01, AGT-02, AGT-03, AGT-04, AGT-05
**Success Criteria** (what must be TRUE):
  1. An Admin can create, update, and retrieve agent profiles via the API; requests from non-Admin roles receive 403
  2. `GET /api/agents` returns all agent profiles with each agent's current weighted load (active ticket count weighted by priority)
  3. `GET /api/agents/{id}` returns a single agent profile including load details
  4. Admin can toggle agent availability and update max capacity via `PUT /api/agents/{id}`, and the algorithm immediately reflects the change on the next assignment
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Domain and Persistence Foundation | 1/2 | In progress | - |
| 2. Assignment Algorithm | 0/TBD | Not started | - |
| 3. Event Publishing | 0/TBD | Not started | - |
| 4. Worker Service | 0/TBD | Not started | - |
| 5. Docker Integration | 0/TBD | Not started | - |
| 6. Admin and Operational APIs | 0/TBD | Not started | - |
