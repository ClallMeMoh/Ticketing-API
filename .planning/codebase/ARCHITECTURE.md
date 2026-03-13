# Architecture

**Analysis Date:** 2026-03-13

## Pattern Overview

**Overall:** Clean Architecture with DDD (Domain-Driven Design) and CQRS (Command Query Responsibility Segregation)

**Key Characteristics:**
- Four-layer vertical structure with clear separation of concerns
- MediatR handles all application requests (commands and queries)
- Aggregate root pattern applied to Ticket entity
- Transactional consistency through Unit of Work pattern
- Separate read and write models for queries
- Explicit exception hierarchy for domain and application errors

## Layers

**Domain Layer (`src/Ticketing.Domain/`):**
- Purpose: Define core business logic, entities, and domain rules
- Location: `src/Ticketing.Domain/`
- Contains: Entities (AppUser, Ticket, Comment), Enums (UserRole, TicketStatus, TicketPriority), Domain exceptions, Repository interfaces
- Depends on: Nothing (pure business logic)
- Used by: Application, Infrastructure layers

**Application Layer (`src/Ticketing.Application/`):**
- Purpose: Orchestrate business processes using CQRS pattern
- Location: `src/Ticketing.Application/`
- Contains: Commands, Queries, Handlers, DTOs, Validators, Application interfaces (ITicketReadService, ICurrentUserService, IJwtTokenGenerator, IPasswordHasher), ValidationBehavior pipeline
- Depends on: Domain layer, MediatR, FluentValidation
- Used by: API layer, Infrastructure (dependency injection)

**Infrastructure Layer (`src/Ticketing.Infrastructure/`):**
- Purpose: Implement technical concerns and persistence
- Location: `src/Ticketing.Infrastructure/`
- Contains: ApplicationDbContext, EF Core configurations, Repository implementations, Services (PasswordHasher, JwtTokenGenerator, CurrentUserService), Database seeder
- Depends on: Domain, Application layers, EF Core, Microsoft.AspNetCore.Authentication.JwtBearer
- Used by: API layer

**API Layer (`src/Ticketing.API/`):**
- Purpose: HTTP entry point and cross-cutting concerns
- Location: `src/Ticketing.API/`
- Contains: Controllers (AuthController, TicketsController, CommentsController, UsersController), Middleware (ExceptionHandlingMiddleware), Program.cs with dependency injection and configuration
- Depends on: Application, Infrastructure layers, ASP.NET Core
- Used by: HTTP clients

## Data Flow

**Write Flow (Commands):**

1. HTTP request → Controller method
2. Controller creates Command object (e.g., `CreateTicketCommand`)
3. Controller sends Command via `IMediator.Send()`
4. MediatR routes to corresponding Handler (e.g., `CreateTicketCommandHandler`)
5. Handler validates through FluentValidation via `ValidationBehavior<,>` pipeline
6. Handler accesses Repository interface via dependency injection
7. Handler instantiates or modifies Domain entity with business logic
8. Handler stores entity reference and calls `IUnitOfWork.SaveChangesAsync()`
9. `ApplicationDbContext.SaveChangesAsync()` applies audit fields, calls EF Core
10. Response returned to Controller, serialized to JSON

**Example:** `POST /api/tickets` with CreateTicketCommand:
```
POST → TicketsController.Create()
→ IMediator.Send(CreateTicketCommand)
→ ValidationBehavior validates CreateTicketCommandValidator rules
→ CreateTicketCommandHandler.Handle()
→ new Ticket(...) instantiates aggregate
→ ITicketRepository.AddAsync(ticket)
→ IUnitOfWork.SaveChangesAsync()
→ ApplicationDbContext sets audit fields
→ EF Core persists to SQL Server
→ Guid returned to Controller
→ 201 Created response
```

**Read Flow (Queries):**

1. HTTP request → Controller method
2. Controller creates Query object (e.g., `GetTicketsQuery` with pagination/filter parameters)
3. Controller sends Query via `IMediator.Send()`
4. MediatR routes to corresponding Handler (e.g., `GetTicketsQueryHandler`)
5. Handler delegates to Read Service (e.g., `ITicketReadService`)
6. Read Service executes optimized LINQ query with `AsNoTracking()` (no tracking overhead)
7. Read Service projects to DTO response objects directly in query
8. `PagedResponse<T>` wrapper returned with items, total count, calculated pages
9. Response serialized to JSON

**Example:** `GET /api/tickets?pageNumber=1&status=Open` with GetTicketsQuery:
```
GET → TicketsController.GetAll()
→ IMediator.Send(GetTicketsQuery)
→ GetTicketsQueryHandler.Handle()
→ ITicketReadService.GetPagedAsync()
→ LINQ query with WHERE, SKIP, TAKE, SELECT
→ AsNoTracking() prevents change tracking
→ PagedResponse<TicketResponse> returned
→ JSON serialized with camelCase
```

**State Management:**

- Entity state tracked by EF Core DbContext only during write operations
- Read operations use `AsNoTracking()` to avoid unnecessary state tracking
- Aggregate modifications done through domain methods (e.g., `Ticket.AssignTo()`, `Ticket.ChangeStatus()`)
- Unit of Work commits all changes atomically
- Audit fields set automatically in `ApplicationDbContext.SetAuditFields()` on every SaveChangesAsync

## Key Abstractions

**Aggregate Root (Ticket):**
- Purpose: Represents coherent business entity with internal consistency rules
- Examples: `src/Ticketing.Domain/Entities/Ticket.cs`
- Pattern: Private collection `_comments` exposed as read-only collection, Methods enforce business rules before state changes (AssignTo, ChangeStatus, UpdateDetails, Close, Reopen), Status transitions validated

**Unit of Work (IUnitOfWork):**
- Purpose: Coordinate transactions and ensure all repository operations commit together
- Examples: `src/Ticketing.Domain/Repositories/IUnitOfWork.cs` (interface), `src/Ticketing.Infrastructure/Persistence/ApplicationDbContext.cs` (implements)
- Pattern: Single SaveChangesAsync call persists all entity changes atomically

**Repository Pattern:**
- Purpose: Isolate data access logic and provide domain-focused API
- Examples: `ITicketRepository`, `IUserRepository`, `ICommentRepository` (interfaces in Domain), `TicketRepository`, `UserRepository`, `CommentRepository` (implementations in Infrastructure)
- Pattern: Generic Add/Update/Delete methods, Specialized GetById/GetByIdWithComments methods, Write repositories return entities, Read services return DTOs

**Read Service Pattern:**
- Purpose: Separate optimized read logic from aggregate-focused write repositories
- Examples: `ITicketReadService`, `ICommentReadService` (interfaces in Application), `TicketReadService`, `CommentReadService` (implementations in Infrastructure)
- Pattern: Returns PagedResponse with pagination and filtering already applied, Uses AsNoTracking for performance, Projects directly to DTOs (not entities), CQRS separation keeps writes and reads independent

**Command/Query Separation:**
- Purpose: Explicitly separate state-mutating operations from data retrieval
- Examples: Commands (CreateTicketCommand, UpdateTicketCommand), Queries (GetTicketsQuery, GetTicketByIdQuery)
- Pattern: Commands return identifiers or void, Queries return data transfer objects, Each has dedicated handler via MediatR, FluentValidation validators per command

**Validation Pipeline (ValidationBehavior):**
- Purpose: Cross-cutting validation that applies to all commands automatically
- Examples: `src/Ticketing.Application/Common/ValidationBehavior.cs`
- Pattern: Implements `IPipelineBehavior<,>` from MediatR, Registered in DependencyInjection.cs, Validates before handler executes

## Entry Points

**HTTP Entry Point (Program.cs):**
- Location: `src/Ticketing.API/Program.cs`
- Triggers: Application startup
- Responsibilities: Configure ASP.NET Core services (Controllers, Swagger, Authentication), Register Application and Infrastructure dependencies, Apply middleware (ExceptionHandling), Auto-migrate database, Seed test data

**Command Entry Points (Controllers):**
- POST `/api/auth/register` → `RegisterUserCommand` (AuthController)
- POST `/api/auth/login` → `LoginUserCommand` (AuthController)
- POST `/api/tickets` → `CreateTicketCommand` (TicketsController)
- PUT `/api/tickets/{id}` → `UpdateTicketCommand` (TicketsController)
- PUT `/api/tickets/{id}/assign` → `AssignTicketCommand` (TicketsController)
- PUT `/api/tickets/{id}/status` → `ChangeTicketStatusCommand` (TicketsController)
- DELETE `/api/tickets/{id}` → `DeleteTicketCommand` (TicketsController)
- POST `/api/comments` → `AddCommentCommand` (CommentsController)
- PUT `/api/users/{id}/role` → `ChangeUserRoleCommand` (UsersController)

**Query Entry Points (Controllers):**
- GET `/api/tickets/{id}` → `GetTicketByIdQuery` (TicketsController)
- GET `/api/tickets` → `GetTicketsQuery` with filtering/pagination (TicketsController)
- GET `/api/tickets/mine` → `GetMyTicketsQuery` (TicketsController)
- GET `/api/tickets/assigned` → `GetAssignedTicketsQuery` (TicketsController)
- GET `/api/comments/{ticketId}` → `GetCommentsByTicketIdQuery` (CommentsController)
- GET `/api/users` → `GetAllUsersQuery` (UsersController)

## Error Handling

**Strategy:** Three-tier exception hierarchy with dedicated middleware for HTTP response mapping

**Exception Types:**

- `DomainException` (`src/Ticketing.Domain/Exceptions/DomainException.cs`): Thrown by domain methods when business rules violated. Caught by middleware → 400 Bad Request
- `NotFoundException` (`src/Ticketing.Application/Exceptions/NotFoundException.cs`): Thrown when entity not found. Caught by middleware → 404 Not Found
- `ForbiddenAccessException` (`src/Ticketing.Application/Exceptions/ForbiddenAccessException.cs`): Thrown when user lacks authorization. Caught by middleware → 403 Forbidden
- `UnauthorizedException` (`src/Ticketing.Application/Exceptions/UnauthorizedException.cs`): Thrown when authentication fails. Caught by middleware → 401 Unauthorized
- `ValidationException` (from FluentValidation): Thrown by ValidationBehavior pipeline. Caught by middleware → 400 Bad Request with field errors
- Unhandled exceptions → 500 Internal Server Error

**Patterns:**
- Domain methods validate state and throw `DomainException` (e.g., `Ticket.ChangeStatus()` validates status transitions)
- Handlers check preconditions and throw `NotFoundException` or `ForbiddenAccessException` (e.g., handler verifies ticket exists and user has permission)
- `ExceptionHandlingMiddleware` intercepts all exceptions, logs them, maps to appropriate HTTP responses
- All error responses follow consistent shape: `{ error: string, message: string }`

## Cross-Cutting Concerns

**Logging:** Middleware logs unhandled exceptions via `ILogger<ExceptionHandlingMiddleware>`. Domain and handlers may log via constructor-injected ILogger if needed. Error middleware logs with `_logger.LogError(ex, "Unhandled exception occurred.")`

**Validation:** Fluent Validation framework with validators per command in `Application` layer. `ValidationBehavior` pipeline automatically validates all commands before handler execution. Validators enforce: required fields, string length, enum value bounds, GUID non-zero requirements, cross-field rules

**Authentication:** JWT Bearer tokens configured in `DependencyInjection.cs`. Tokens validated on every request via ASP.NET Core authentication middleware. Controllers use `[Authorize]` attribute to require authentication. Role claims extracted from token and used for `[Authorize(Roles = "Admin,Agent")]`

**Authorization:** Role-based access control enforced declaratively via `[Authorize(Roles = "...")]` attributes on controller actions (e.g., only Admin can delete tickets, only Admin/Agent can assign). Handlers may perform additional authorization checks (e.g., `CurrentUserService` tracks authenticated user, handlers verify ownership/role before allowing mutations)

**Audit Fields:** `ApplicationDbContext.SaveChangesAsync()` calls `SetAuditFields()` which iterates all `AuditableEntity` entries, sets `CreatedAt` on insert, sets `UpdatedAt` on update. Automatic and non-nullable on all auditable entities

---

*Architecture analysis: 2026-03-13*
