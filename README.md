# Ticketing API

A ticket management system API built with .NET 10, following DDD architecture with CQRS and event-driven auto-assignment.

## What It Does

- Users register and log in with JWT authentication
- Authenticated users create and manage support tickets
- Agents handle assigned tickets
- Admins manage users, assign tickets, and perform privileged actions
- Tickets support status changes, comments, pagination, and filtering
- New tickets are auto-assigned by a worker service through RabbitMQ
- Admins can manage agent availability/capacity and view weighted load

## Tech Stack

- .NET 10 / ASP.NET Core
- Entity Framework Core 10 with SQL Server
- MediatR (CQRS)
- MassTransit + RabbitMQ
- FluentValidation
- JWT Bearer Authentication
- BCrypt password hashing
- Swagger / Swashbuckle
- Docker + Docker Compose (API + Worker + SQL Server + RabbitMQ)
- xUnit + NSubstitute

## Architecture

```
src/
  Ticketing.Domain          -- Entities, enums, business rules, repository interfaces
  Ticketing.Application     -- Commands, queries, handlers, DTOs, validators, read service interfaces
  Ticketing.Infrastructure  -- EF Core, repositories, read services, JWT, password hashing, messaging
  Ticketing.API             -- Controllers, middleware, Swagger, startup config
  Ticketing.Worker          -- RabbitMQ consumer and reconciliation background service
tests/
  Ticketing.Tests           -- Unit tests for domain rules and handlers
```

Key patterns:
- CQRS with MediatR -- commands mutate state, queries return data
- Read/write separation -- repositories handle entity persistence, read services project directly to DTOs using `Select()`
- FluentValidation pipeline behavior for automatic request validation
- Role-based authorization at both controller and handler levels
- Dedicated exception types mapped to HTTP status codes (400, 401, 403, 404)
- Sequential GUIDs (`NEWSEQUENTIALID()`) for SQL Server index performance
- `TimeProvider` abstraction for testable audit fields
- Database seeder for bootstrapping admin user on first startup
- Event-driven assignment pipeline (`TicketCreatedEvent` -> worker consumer -> auto-assignment command)
- Reconciliation sweep to recover missed assignment events

## New Auto-Assignment Features

- `Assigned` ticket status added between `Open` and `InProgress`
- `AgentProfiles` table for availability, capacity, and efficiency
- `AssignmentHistories` table for manual/auto assignment audit trails
- Concurrency protection on tickets with `RowVersion`
- Weighted load-balancing algorithm:
  - Priority weights: Low=1, Medium=2, High=3, Critical=5
  - Ranking: projected load ratio -> least recently assigned -> higher efficiency -> deterministic user id
- Admin agent profile APIs:
  - `GET /api/agents`
  - `GET /api/agents/{userId}`
  - `POST /api/agents`
  - `PUT /api/agents/{userId}`

## How Auto-Assignment Works (Simple)

1. A user creates a ticket.
2. The API saves the ticket, then publishes a `TicketCreatedEvent`.
3. RabbitMQ carries that event to the worker service.
4. The worker receives the event and runs the auto-assignment command.
5. The algorithm picks the best available agent using:
   - current active load
   - agent capacity (`MaxConcurrentTickets`)
   - ticket priority weight (Low=1, Medium=2, High=3, Critical=5)
6. The ticket is assigned and its status moves to `Assigned`.
7. Assignment history is saved for audit.

Safety net:
- A reconciliation background job periodically checks for old unassigned open tickets and retries assignment.

## Default Admin

On startup, the API seeds a default admin user if one doesn't exist:

- Email: `admin@test.com`
- Password: `Admin123!`

Use this account to perform admin actions like assigning tickets or deleting them.

## Running Locally

Prerequisites: .NET 10 SDK, SQL Server

1. Update the connection string in `src/Ticketing.API/appsettings.json`
2. Run the API:
   ```
   dotnet run --project src/Ticketing.API
   ```
3. Open Swagger at `http://localhost:<port>/swagger`

The database and migrations are applied automatically on startup.

## Running with Docker

Prerequisites: Docker and Docker Compose

```
docker compose up --build
```

This starts API, Worker, SQL Server, and RabbitMQ in one stack.

- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`
- SQL Server: `localhost:1433`
- RabbitMQ AMQP: `localhost:5673`
- RabbitMQ Management UI: `http://localhost:15673`

The database and migrations are applied automatically on startup.

## API Endpoints

### Auth
- `POST /api/auth/register` -- Register a new user
- `POST /api/auth/login` -- Log in and receive a JWT token

### Tickets
- `POST /api/tickets` -- Create a ticket
- `GET /api/tickets` -- List tickets (supports pagination and filtering by status, priority, assigned user, creator, title search)
- `GET /api/tickets/{id}` -- Get a ticket by ID
- `GET /api/tickets/mine` -- Get tickets created by current user
- `GET /api/tickets/assigned` -- Get tickets assigned to current user
- `PUT /api/tickets/{id}` -- Update a ticket
- `PUT /api/tickets/{id}/assign` -- Assign a ticket to an agent (admin/agent only)
- `PUT /api/tickets/{id}/status` -- Change ticket status
- `DELETE /api/tickets/{id}` -- Delete a ticket (admin only)

### Comments
- `POST /api/tickets/{ticketId}/comments` -- Add a comment
- `GET /api/tickets/{ticketId}/comments` -- Get comments for a ticket (paginated)

### Agent Profiles (Admin)
- `GET /api/agents` -- List agent profiles with load metrics
- `GET /api/agents/{userId}` -- Get one profile with load metrics
- `POST /api/agents` -- Create agent profile for agent user
- `PUT /api/agents/{userId}` -- Update availability/capacity/efficiency

## Error Handling

The API uses structured error responses with semantically correct HTTP status codes:

| Exception | Status Code | Use Case |
|-----------|-------------|----------|
| `ValidationException` | 400 | Invalid request data |
| `DomainException` | 400 | Business rule violation |
| `UnauthorizedException` | 401 | Invalid credentials |
| `ForbiddenAccessException` | 403 | Insufficient permissions |
| `NotFoundException` | 404 | Entity not found |

## Running Tests

```
dotnet test
```

Unit tests cover domain rules, CQRS handlers, and auto-assignment paths.

## Troubleshooting

- Swagger says "Unable to render this definition":
  - Use `http://localhost:5000/swagger/index.html` (not HTTPS in local Docker)
  - Hard refresh (`Ctrl+F5`) if browser cache is stale
- RabbitMQ port conflict on `5672`:
  - This project maps RabbitMQ to `5673` / `15673` by default
- Local API-only run without RabbitMQ license/setup:
  - Set `Messaging__DisableMassTransit=true` to use no-op message publishing for quick local endpoint testing
