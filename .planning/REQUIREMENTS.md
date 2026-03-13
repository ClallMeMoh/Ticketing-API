# Requirements: Ticket Auto-Assignment System

**Defined:** 2026-03-13
**Core Value:** Tickets are automatically assigned to the right agent within seconds of creation, distributing work fairly based on load, capacity, and efficiency.

## v1 Requirements

### Domain Foundation

- [x] **DOM-01**: Ticket lifecycle includes `Assigned` status between `Open` and `InProgress`
- [x] **DOM-02**: `Ticket.AssignTo()` sets status to `Assigned` instead of `InProgress`
- [x] **DOM-03**: Status transitions handle `Assigned` correctly (Assigned → InProgress, Assigned → Open, Closed cannot go to Assigned)
- [x] **DOM-04**: AgentProfile entity tracks `IsAvailable`, `MaxConcurrentTickets`, `LastAssignedAt`, `EfficiencyScore` per agent
- [ ] **DOM-05**: TicketAssignmentHistory entity logs every assignment with type (Manual/Auto), reason, and timestamp
- [x] **DOM-06**: RowVersion concurrency token on Ticket is configured in EF Core (fix missing `.IsRowVersion()`)
- [ ] **DOM-07**: EF migration includes data migration shifting existing status values for new `Assigned` enum position

### Messaging

- [ ] **MSG-01**: `TicketCreatedEvent` is published to RabbitMQ after successful ticket creation
- [ ] **MSG-02**: RabbitMQ exchange `ticketing.events` with queue `ticket-assignment` and routing key `ticket.created`
- [ ] **MSG-03**: Worker service consumes messages from `ticket-assignment` queue
- [ ] **MSG-04**: RabbitMQ connection retries with exponential backoff on transient failures
- [ ] **MSG-05**: Worker acknowledges messages only after successful processing

### Algorithm

- [ ] **ALG-01**: Algorithm filters agents by: role = Agent, IsAvailable = true, active ticket count < MaxConcurrentTickets
- [ ] **ALG-02**: Weighted load calculated using priority weights: Low=1, Medium=2, High=3, Critical=5
- [ ] **ALG-03**: Agents ranked by: projected load ratio ascending → LastAssignedAt ascending → EfficiencyScore descending → UserId ascending
- [ ] **ALG-04**: Assignment writes TicketAssignmentHistory record with type Auto and structured reason string
- [ ] **ALG-05**: When no eligible agent exists, ticket stays Open with warning logged
- [ ] **ALG-06**: Double-assignment prevented by catching DbUpdateConcurrencyException (RowVersion)
- [ ] **ALG-07**: Algorithm executes in 2 queries max, no N+1 loops

### Worker Service

- [ ] **WRK-01**: `Ticketing.Worker` project as .NET Worker Service referencing Application and Infrastructure
- [ ] **WRK-02**: Worker reuses existing DI registration (Infrastructure split to exclude JWT/HTTP concerns)
- [ ] **WRK-03**: Consumer deserializes `TicketCreatedEvent` and dispatches `AutoAssignTicketCommand` via MediatR
- [ ] **WRK-04**: Reconciliation sweep runs on configurable interval (default 60s) catching unassigned tickets older than threshold
- [ ] **WRK-05**: Worker failures are logged with structured logging, do not crash the service

### Agent Management

- [ ] **AGT-01**: Admin can create agent profile for an Agent-role user via API
- [ ] **AGT-02**: Admin can update agent availability and max capacity via API
- [ ] **AGT-03**: Admin can view all agent profiles with current weighted load via API
- [ ] **AGT-04**: Admin can view single agent profile with load details via API
- [ ] **AGT-05**: Agent management endpoints are protected with Admin role authorization

### Docker

- [ ] **DOC-01**: RabbitMQ service added to docker-compose with management UI (ports 5672, 15672) and health check
- [ ] **DOC-02**: Worker service added to docker-compose depending on RabbitMQ and SQL Server
- [ ] **DOC-03**: `docker-compose up --build` starts API, Worker, SQL Server, and RabbitMQ together
- [ ] **DOC-04**: Worker Dockerfile created for the worker service

## v2 Requirements

### Notifications

- **NTF-01**: Agent receives notification when ticket is auto-assigned to them
- **NTF-02**: Admin receives notification when no agent is available for assignment

### Advanced Assignment

- **ADV-01**: Skill-based routing matches ticket categories to agent expertise
- **ADV-02**: Assignment history endpoint for auditing per-ticket assignment chain

### Monitoring

- **MON-01**: Worker health check endpoint for Docker/orchestrator integration
- **MON-02**: Assignment metrics dashboard (avg assignment time, agent utilization)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Real-time WebSocket push on assignment | High implementation cost, low milestone value |
| Multi-queue or topic routing | One exchange, one queue sufficient at this scale |
| ML-based weight tuning | Over-engineering for internal business system |
| Dead-letter queue handling | Reconciliation sweep covers same failure class more simply |
| Agent self-selection / preference routing | Algorithm handles fairness objectively |
| Separate read database | Premature optimization for current scale |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| DOM-01 | Phase 1 | Pending |
| DOM-02 | Phase 1 | Pending |
| DOM-03 | Phase 1 | Pending |
| DOM-04 | Phase 1 | Pending |
| DOM-05 | Phase 1 | Pending |
| DOM-06 | Phase 1 | Pending |
| DOM-07 | Phase 1 | Pending |
| ALG-01 | Phase 2 | Pending |
| ALG-02 | Phase 2 | Pending |
| ALG-03 | Phase 2 | Pending |
| ALG-04 | Phase 2 | Pending |
| ALG-05 | Phase 2 | Pending |
| ALG-06 | Phase 2 | Pending |
| ALG-07 | Phase 2 | Pending |
| MSG-01 | Phase 3 | Pending |
| MSG-02 | Phase 3 | Pending |
| MSG-03 | Phase 4 | Pending |
| MSG-04 | Phase 4 | Pending |
| MSG-05 | Phase 4 | Pending |
| WRK-01 | Phase 4 | Pending |
| WRK-02 | Phase 4 | Pending |
| WRK-03 | Phase 4 | Pending |
| WRK-04 | Phase 4 | Pending |
| WRK-05 | Phase 4 | Pending |
| DOC-01 | Phase 5 | Pending |
| DOC-02 | Phase 5 | Pending |
| DOC-03 | Phase 5 | Pending |
| DOC-04 | Phase 5 | Pending |
| AGT-01 | Phase 6 | Pending |
| AGT-02 | Phase 6 | Pending |
| AGT-03 | Phase 6 | Pending |
| AGT-04 | Phase 6 | Pending |
| AGT-05 | Phase 6 | Pending |

**Coverage:**
- v1 requirements: 33 total
- Mapped to phases: 33
- Unmapped: 0

---
*Requirements defined: 2026-03-13*
*Last updated: 2026-03-13 after roadmap creation*
