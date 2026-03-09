# Ticketing API

A ticket management system API built with .NET 10, following DDD architecture with CQRS.

## What It Does

- Users register and log in with JWT authentication
- Authenticated users create and manage support tickets
- Agents handle assigned tickets
- Admins manage users, assign tickets, and perform privileged actions
- Tickets support status changes, comments, pagination, and filtering

## Tech Stack

- .NET 10 / ASP.NET Core
- Entity Framework Core 10 with SQL Server
- MediatR (CQRS)
- FluentValidation
- JWT Bearer Authentication
- BCrypt password hashing
- Swagger / Swashbuckle
- Docker + Docker Compose
- xUnit + NSubstitute

## Architecture

```
src/
  Ticketing.Domain          -- Entities, enums, business rules, repository interfaces
  Ticketing.Application     -- Commands, queries, handlers, DTOs, validators, read service interfaces
  Ticketing.Infrastructure  -- EF Core, repositories, read services, JWT, password hashing
  Ticketing.API             -- Controllers, middleware, Swagger, startup config
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
3. Open Swagger at `https://localhost:<port>/swagger`

The database and migrations are applied automatically on startup.

## Running with Docker

Prerequisites: Docker and Docker Compose

```
docker-compose up --build
```

This starts the API on port 5000 and SQL Server on port 1433. The database is created automatically on startup.

Swagger: `http://localhost:5000/swagger`

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

25 unit tests covering domain business rules, command handlers, and validators.
