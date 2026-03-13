# Ticket Management System — Auto-Assignment Extension

## What This Is

A .NET Ticket Management System API built with DDD, CQRS/MediatR, JWT auth, and role-based authorization. The existing system handles ticket CRUD, comments, user management, pagination, and filtering. This milestone adds intelligent auto-assignment: when a ticket is created, a worker service consumes a RabbitMQ event and assigns it to the best available agent using a weighted load-balancing algorithm.

## Core Value

Tickets are automatically assigned to the right agent within seconds of creation, distributing work fairly based on agent load, capacity, and efficiency.

## Requirements

### Validated

- ✓ User registration and login with JWT — existing
- ✓ Role-based authorization (User, Agent, Admin) — existing
- ✓ Ticket CRUD (create, read, update, delete) — existing
- ✓ Ticket status transitions (Open, InProgress, Resolved, Closed) with business rules — existing
- ✓ Ticket assignment (manual, by Admin/Agent) — existing
- ✓ Ticket queries with pagination and filtering (status, priority, assignee, creator, title search) — existing
- ✓ Comments on tickets with authorization — existing
- ✓ Audit fields (CreatedAt, UpdatedAt) on all entities — existing
- ✓ Swagger with JWT bearer auth — existing
- ✓ Docker support (API + SQL Server) — existing
- ✓ Global exception handling middleware — existing
- ✓ FluentValidation pipeline — existing

### Active

- [ ] Add `Assigned` status to ticket lifecycle between Open and InProgress
- [ ] AgentProfile entity tracking availability, capacity, efficiency, and last assignment
- [ ] TicketAssignmentHistory entity logging all assignments with type (Manual/Auto) and reason
- [ ] Concurrency protection (RowVersion) on Ticket to prevent double-assignment
- [ ] EF Core migration for new tables and status value shift
- [ ] RabbitMQ messaging: publish TicketCreatedEvent on ticket creation
- [ ] Worker service consuming assignment queue
- [ ] Weighted load-balancing algorithm (priority weights: Low=1, Medium=2, High=3, Critical=5)
- [ ] Agent ranking: projected load ratio → idle time → efficiency score → deterministic fallback
- [ ] Reconciliation worker as safety net for missed events
- [ ] Agent profile management endpoints (Admin-only CRUD)
- [ ] Agent load visibility through API
- [ ] Worker Docker integration (docker-compose with RabbitMQ + Worker)

### Out of Scope

- Notification system — separate concern, not part of assignment
- Real-time WebSocket updates — adds complexity without core value
- Multi-queue routing topology — one exchange, one queue is sufficient
- Agent self-assignment or preference-based routing — keep algorithm simple
- Email/SMS alerts on assignment — defer to future milestone

## Context

- Existing codebase follows clean 4-layer DDD: Domain → Application → Infrastructure → API
- CQRS with MediatR is established — commands mutate, queries read
- Repository + Unit of Work pattern in place
- Read services use AsNoTracking with DTO projection
- SQL Server as database, Docker Compose for local dev
- Phase 14 (domain extensions) is partially implemented with uncommitted changes
- The worker service will be a separate .NET project sharing Application and Infrastructure layers

## Constraints

- **Tech stack**: .NET, EF Core, SQL Server, RabbitMQ — matches existing stack
- **Architecture**: Must follow established DDD/CQRS patterns — no new architectural paradigms
- **Complexity**: Worker is an orchestrator, not a business logic container — keep it lean
- **Performance**: Assignment algorithm must use 2 queries max, no N+1 loops
- **Messaging**: Simple RabbitMQ setup — one exchange (`ticketing.events`), one queue (`ticket-assignment`)
- **Docker**: All services must run together via single `docker-compose up`

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| RabbitMQ over in-process background service | Decouples assignment from API request lifecycle, survives API restarts | — Pending |
| Weighted load algorithm over round-robin | Accounts for ticket priority differences and agent capacity | — Pending |
| Reconciliation worker as safety net | Events can be lost; periodic sweep catches unassigned tickets | — Pending |
| Separate Worker project over hosted service in API | Clean separation, independent scaling, independent deployment | — Pending |
| RowVersion concurrency token | Prevents race conditions in double-assignment scenarios | — Pending |

---
*Last updated: 2026-03-13 after initialization*
