# Codebase Structure

**Analysis Date:** 2026-03-13

## Directory Layout

```
src/
├── Ticketing.Domain/
│   ├── Common/
│   │   ├── BaseEntity.cs              # Base class with Id property
│   │   └── AuditableEntity.cs         # Base class with CreatedAt, UpdatedAt
│   ├── Entities/
│   │   ├── AppUser.cs                 # User aggregate root
│   │   ├── Ticket.cs                  # Ticket aggregate root
│   │   ├── Comment.cs                 # Comment entity
│   │   ├── AgentProfile.cs            # Agent profile tracking
│   │   └── TicketAssignmentHistory.cs # Assignment history log
│   ├── Enums/
│   │   ├── UserRole.cs                # User, Agent, Admin
│   │   ├── TicketStatus.cs            # Open, Assigned, InProgress, Closed
│   │   ├── TicketPriority.cs          # Low, Medium, High, Critical
│   │   └── AssignmentType.cs          # DirectAssignment, AutoAssignment, etc
│   ├── Exceptions/
│   │   └── DomainException.cs         # Base domain rule violation
│   └── Repositories/
│       ├── IUnitOfWork.cs             # Transaction coordinator interface
│       ├── ITicketRepository.cs       # Ticket write operations interface
│       ├── IUserRepository.cs         # User write operations interface
│       ├── ICommentRepository.cs      # Comment write operations interface
│       ├── IAgentProfileRepository.cs # Agent profile operations
│       └── IAssignmentHistoryRepository.cs # Assignment history operations
│
├── Ticketing.Application/
│   ├── Auth/
│   │   ├── Commands/
│   │   │   ├── Register/
│   │   │   │   ├── RegisterUserCommand.cs
│   │   │   │   ├── RegisterUserCommandHandler.cs
│   │   │   │   └── RegisterUserCommandValidator.cs
│   │   │   └── Login/
│   │   │       ├── LoginUserCommand.cs
│   │   │       ├── LoginUserCommandHandler.cs
│   │   │       └── LoginUserCommandValidator.cs
│   │   └── DTOs/
│   │       └── AuthResponse.cs        # Login/register response
│   ├── Tickets/
│   │   ├── Commands/
│   │   │   ├── CreateTicket/
│   │   │   ├── UpdateTicket/
│   │   │   ├── AssignTicket/
│   │   │   ├── ChangeTicketStatus/
│   │   │   └── DeleteTicket/
│   │   ├── Queries/
│   │   │   ├── GetTicketById/
│   │   │   ├── GetTickets/
│   │   │   ├── GetMyTickets/
│   │   │   └── GetAssignedTickets/
│   │   └── DTOs/
│   │       └── TicketResponse.cs      # Ticket query response
│   ├── Comments/
│   │   ├── Commands/
│   │   │   └── AddComment/
│   │   ├── Queries/
│   │   │   └── GetCommentsByTicketId/
│   │   └── DTOs/
│   │       └── CommentResponse.cs
│   ├── Users/
│   │   ├── Commands/
│   │   │   └── ChangeUserRole/
│   │   ├── Queries/
│   │   │   └── GetAllUsers/
│   │   └── DTOs/
│   │       └── UserDto.cs
│   ├── Common/
│   │   ├── PagedResponse.cs           # Generic pagination wrapper
│   │   └── ValidationBehavior.cs      # MediatR validation pipeline
│   ├── Exceptions/
│   │   ├── NotFoundException.cs
│   │   ├── ForbiddenAccessException.cs
│   │   └── UnauthorizedException.cs
│   ├── Interfaces/
│   │   ├── ICurrentUserService.cs     # Get authenticated user info
│   │   ├── IJwtTokenGenerator.cs      # Generate JWT tokens
│   │   ├── IPasswordHasher.cs         # Hash passwords
│   │   ├── ITicketReadService.cs      # Ticket query service
│   │   └── ICommentReadService.cs     # Comment query service
│   └── DependencyInjection.cs         # App layer registration
│
├── Ticketing.Infrastructure/
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs    # EF Core DbContext
│   │   ├── Configurations/
│   │   │   ├── AppUserConfiguration.cs
│   │   │   ├── TicketConfiguration.cs
│   │   │   └── CommentConfiguration.cs
│   │   ├── Migrations/
│   │   │   ├── 20260308181026_InitialCreate.cs
│   │   │   ├── 20260309172411_SequentialGuids.cs
│   │   │   └── ApplicationDbContextModelSnapshot.cs
│   │   └── DatabaseSeeder.cs          # Test data population
│   ├── Repositories/
│   │   ├── TicketRepository.cs        # Ticket write operations
│   │   ├── UserRepository.cs          # User write operations
│   │   ├── CommentRepository.cs       # Comment write operations
│   │   ├── TicketReadService.cs       # Ticket optimized queries
│   │   └── CommentReadService.cs      # Comment optimized queries
│   ├── Services/
│   │   ├── PasswordHasher.cs          # PBKDF2 password hashing
│   │   ├── JwtTokenGenerator.cs       # JWT token creation
│   │   └── CurrentUserService.cs      # Extract user from HttpContext
│   ├── DependencyInjection.cs         # Infra layer registration
│   └── (no public exports)
│
└── Ticketing.API/
    ├── Controllers/
    │   ├── AuthController.cs          # Register, Login endpoints
    │   ├── TicketsController.cs       # Ticket CRUD endpoints
    │   ├── CommentsController.cs      # Comment endpoints
    │   └── UsersController.cs         # User management endpoints
    ├── Middleware/
    │   └── ExceptionHandlingMiddleware.cs # Global exception mapper
    ├── Properties/
    │   └── launchSettings.json        # Launch profile configuration
    ├── Program.cs                     # Entry point and configuration
    └── appsettings.json               # App configuration
```

## Directory Purposes

**`src/Ticketing.Domain/`:**
- Purpose: Pure business domain logic, free of infrastructure dependencies
- Contains: Entities with business behavior, Enums for domain concepts, Domain exceptions, Repository contracts
- Key files: `Entities/Ticket.cs` (main aggregate), `Common/AuditableEntity.cs` (shared base), `Repositories/IUnitOfWork.cs`
- Isolation: No references to EF Core, MediatR, JWT, or ASP.NET

**`src/Ticketing.Application/`:**
- Purpose: Application-specific orchestration and use case implementation
- Contains: CQRS commands and queries, Handlers that coordinate repositories and services, DTOs for API contracts, Validators using FluentValidation, Application-layer exceptions, Service interfaces (implementations in Infrastructure)
- Key files: `DependencyInjection.cs` (MediatR registration), `Common/ValidationBehavior.cs` (pipeline), specific command/query handlers
- Isolation: No references to EF Core, Controllers, or HTTP concerns

**`src/Ticketing.Infrastructure/`:**
- Purpose: Technical implementations of domain contracts and application interfaces
- Contains: EF Core DbContext and configurations, Repository implementations, Service implementations (password hashing, JWT generation, current user extraction), Database seeding, Authentication setup
- Key files: `Persistence/ApplicationDbContext.cs` (EF core), `DependencyInjection.cs` (registration), `Services/` directory implementations
- Isolation: Contains all EF Core references, but no Controllers or HTTP handling

**`src/Ticketing.API/`:**
- Purpose: HTTP entry point and cross-cutting middleware
- Contains: ASP.NET Core Controllers as thin dispatchers to MediatR, Exception handling middleware, Program.cs with full service configuration
- Key files: `Program.cs` (startup), `Controllers/TicketsController.cs` (example controller), `Middleware/ExceptionHandlingMiddleware.cs`
- Isolation: Entry point layer, can reference all layers

## Key File Locations

**Entry Points:**
- `src/Ticketing.API/Program.cs`: Application startup, service registration, middleware pipeline, database migrations
- `src/Ticketing.API/Controllers/AuthController.cs`: Authentication endpoints (register, login)
- `src/Ticketing.API/Controllers/TicketsController.cs`: Main ticket management endpoints

**Configuration:**
- `src/Ticketing.API/appsettings.json`: Database connection, JWT settings, Swagger config
- `src/Ticketing.Infrastructure/Persistence/ApplicationDbContext.cs`: EF Core model configuration, Audit field setting

**Core Logic:**
- `src/Ticketing.Domain/Entities/Ticket.cs`: Ticket business logic (status transitions, assignments, validation)
- `src/Ticketing.Domain/Entities/AppUser.cs`: User business logic (role management)
- `src/Ticketing.Application/Tickets/Commands/CreateTicket/CreateTicketCommandHandler.cs`: Create ticket use case
- `src/Ticketing.Application/Tickets/Queries/GetTickets/GetTicketsQueryHandler.cs`: Fetch tickets use case

**Testing:**
- `src/Ticketing.Application/Common/ValidationBehavior.cs`: Validator test setup (FluentValidation)
- Individual command/query handlers for unit testing

**Persistence Layer:**
- `src/Ticketing.Infrastructure/Persistence/Configurations/TicketConfiguration.cs`: EF Core model mapping
- `src/Ticketing.Infrastructure/Repositories/TicketRepository.cs`: Ticket data access
- `src/Ticketing.Infrastructure/Repositories/TicketReadService.cs`: Optimized ticket queries

**Authentication/Security:**
- `src/Ticketing.Infrastructure/Services/JwtTokenGenerator.cs`: Token creation and claims
- `src/Ticketing.Infrastructure/Services/PasswordHasher.cs`: Password hashing
- `src/Ticketing.Infrastructure/Services/CurrentUserService.cs`: Authenticated user context

**Error Handling:**
- `src/Ticketing.API/Middleware/ExceptionHandlingMiddleware.cs`: Global exception mapping to HTTP responses
- `src/Ticketing.Application/Exceptions/`: Application exceptions (NotFoundException, ForbiddenAccessException, UnauthorizedException)
- `src/Ticketing.Domain/Exceptions/DomainException.cs`: Business rule violations

## Naming Conventions

**Files:**
- Commands: `[Feature]Command.cs`, `[Feature]CommandHandler.cs`, `[Feature]CommandValidator.cs`
  - Example: `CreateTicketCommand.cs`, `CreateTicketCommandHandler.cs`, `CreateTicketCommandValidator.cs`
- Queries: `[Feature]Query.cs`, `[Feature]QueryHandler.cs`
  - Example: `GetTicketsQuery.cs`, `GetTicketsQueryHandler.cs`
- DTOs: `[Entity]Response.cs` (for query results), or simple names like `AuthResponse.cs`
- Repositories: `[Entity]Repository.cs` for write, `[Entity]ReadService.cs` for read
- Services: `[Capability]Service.cs` (e.g., `PasswordHasher.cs`, `JwtTokenGenerator.cs`)
- Configuration: `[Entity]Configuration.cs` for EF Core configs

**Directories:**
- Feature-based organization: Auth, Tickets, Comments, Users folders group related commands, queries, DTOs
- Operation-based sub-organization: Commands/, Queries/, DTOs/ within features
- Infrastructure organized by responsibility: Persistence/, Repositories/, Services/

**Classes:**
- PascalCase for all classes, records, interfaces, enums
- Commands and Queries use record syntax: `public record CreateTicketCommand(string Title, string Description, ...);`
- Handlers use class syntax: `public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Guid>`
- Interfaces start with I: `ITicketRepository`, `ICurrentUserService`, `IUnitOfWork`

**Properties/Fields:**
- PascalCase for public properties: `public string Title { get; set; }`
- PascalCase for public backing fields in records
- Camelcase (with underscore prefix) for private fields: `private readonly List<Comment> _comments`

**Methods:**
- PascalCase for all public methods
- Domain methods are descriptive: `AssignTo()`, `ChangeStatus()`, `Reopen()` (not `Update()`)

## Where to Add New Code

**New Command (e.g., CloseTicketCommand):**
1. Create folder: `src/Ticketing.Application/Tickets/Commands/CloseTicket/`
2. Create files:
   - `CloseTicketCommand.cs` - record with parameters
   - `CloseTicketCommandHandler.cs` - handler implementing `IRequestHandler<CloseTicketCommand, Unit>`
   - `CloseTicketCommandValidator.cs` - validator implementing `AbstractValidator<CloseTicketCommand>`
3. Add handler logic: Fetch ticket via repository, call domain method (e.g., `ticket.Close()`), save via UnitOfWork
4. Add controller endpoint in `src/Ticketing.API/Controllers/TicketsController.cs`

**New Query (e.g., GetTicketStatsQuery):**
1. Create folder: `src/Ticketing.Application/Tickets/Queries/GetTicketStats/`
2. Create files:
   - `GetTicketStatsQuery.cs` - record with query parameters
   - `GetTicketStatsQueryHandler.cs` - handler implementing `IRequestHandler<GetTicketStatsQuery, TicketStatsResponse>`
3. Create DTO: `src/Ticketing.Application/Tickets/DTOs/TicketStatsResponse.cs`
4. Add handler logic: Delegate to read service or directly query DbContext via dependency injection
5. Add controller endpoint in `src/Ticketing.API/Controllers/TicketsController.cs`

**New Entity (e.g., TicketTag):**
1. Create class in `src/Ticketing.Domain/Entities/TicketTag.cs` extending `AuditableEntity`
2. Add domain methods for business behavior
3. Create repository interface: `src/Ticketing.Domain/Repositories/ITicketTagRepository.cs`
4. Create repository implementation: `src/Ticketing.Infrastructure/Repositories/TicketTagRepository.cs`
5. Create EF Core configuration: `src/Ticketing.Infrastructure/Persistence/Configurations/TicketTagConfiguration.cs`
6. Add DbSet in `ApplicationDbContext.cs`
7. Create migration: `dotnet ef migrations add Add[EntityName]`
8. Create commands/queries as needed in Application layer

**New Feature Domain (e.g., Notifications):**
1. Create domain: `src/Ticketing.Domain/Entities/Notification.cs`
2. Create application folder: `src/Ticketing.Application/Notifications/`
3. Create Commands: `src/Ticketing.Application/Notifications/Commands/SendNotification/`
4. Create Queries: `src/Ticketing.Application/Notifications/Queries/`
5. Create DTOs: `src/Ticketing.Application/Notifications/DTOs/`
6. Create repository: `src/Ticketing.Infrastructure/Repositories/NotificationRepository.cs`
7. Create controller: `src/Ticketing.API/Controllers/NotificationsController.cs`

**Utilities:**
- Shared validation logic: Create in `src/Ticketing.Application/Common/` (e.g., `TicketValidationRules.cs`)
- Shared extensions: Create in `src/Ticketing.Application/Common/` (e.g., `QueryableExtensions.cs`)
- Shared helpers: Create in `src/Ticketing.Infrastructure/` (e.g., `DateTimeHelper.cs`)

## Special Directories

**`src/Ticketing.Infrastructure/Persistence/Migrations/`:**
- Purpose: EF Core database migrations
- Generated: Yes (via `dotnet ef migrations add [name]`)
- Committed: Yes (required for version control and CI/CD)
- Pattern: Each migration has `.cs` file and `.Designer.cs` file with schema snapshot

**`bin/` and `obj/` directories:**
- Purpose: Build output and intermediate compilation
- Generated: Yes
- Committed: No (in .gitignore)

**`src/Ticketing.API/Properties/`:**
- Purpose: Project properties and launch settings
- Contains: `launchSettings.json` (IIS Express, https configuration)
- Committed: Yes

## File Organization Within Layers

**Application Layer Feature Organization:**
Each feature (Auth, Tickets, Comments, Users) has:
```
[Feature]/
├── Commands/
│   └── [Operation]/
│       ├── [Operation]Command.cs
│       ├── [Operation]CommandHandler.cs
│       └── [Operation]CommandValidator.cs
├── Queries/
│   └── [Operation]/
│       ├── [Operation]Query.cs
│       └── [Operation]QueryHandler.cs
└── DTOs/
    └── [Entity]Response.cs
```

This structure groups related use cases by feature, making it easy to locate and extend functionality.

---

*Structure analysis: 2026-03-13*
