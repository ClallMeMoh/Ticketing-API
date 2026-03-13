# Coding Conventions

**Analysis Date:** 2026-03-13

## Naming Patterns

**Files:**
- Command files: `{CommandName}Command.cs` (e.g., `RegisterUserCommand.cs`)
- Handler files: `{CommandName}Handler.cs` (e.g., `RegisterUserCommandHandler.cs`)
- Validator files: `{CommandName}Validator.cs` (e.g., `RegisterUserCommandValidator.cs`)
- Query files: `{QueryName}Query.cs` (e.g., `GetTicketsQuery.cs`)
- Query handler files: `{QueryName}QueryHandler.cs` (e.g., `GetTicketsQueryHandler.cs`)
- Repository files: `{EntityName}Repository.cs` (e.g., `TicketRepository.cs`)
- Service files: `{ServiceName}.cs` (e.g., `JwtTokenGenerator.cs`)
- Entity files: `{EntityName}.cs` (e.g., `Ticket.cs`, `AppUser.cs`)
- Test files: `{ClassToTest}Tests.cs` (e.g., `TicketTests.cs`, `CreateTicketCommandHandlerTests.cs`)

**Functions:**
- PascalCase for all public methods
- Examples: `Handle()`, `GenerateToken()`, `GetByIdAsync()`, `AssignTo()`, `UpdateDetails()`
- Async methods use `Async` suffix: `GetByIdAsync()`, `AddAsync()`, `SaveChangesAsync()`
- Private methods in domain entities use PascalCase (no snake_case distinction): `_handler`, `_context`

**Variables:**
- camelCase for all local variables and parameters
- Examples: `var command = new CreateTicketCommand()`, `string email`, `Guid ticketId`
- Private fields use underscore prefix with camelCase: `_userRepository`, `_handler`, `_validators`, `_comments`
- Read-only collections use underscore prefix: `_comments = new()`

**Types:**
- PascalCase for classes: `Ticket`, `AppUser`, `RegisterUserCommandHandler`
- PascalCase for records (both command/query types and DTOs): `CreateTicketCommand`, `AuthResponse`
- PascalCase for enums: `TicketStatus`, `UserRole`, `TicketPriority`
- Interface names start with `I`: `ITicketRepository`, `IJwtTokenGenerator`, `IUnitOfWork`
- Generic interface parameters are named specifically: `IRequestHandler<RegisterUserCommand, AuthResponse>`

## Code Style

**Formatting:**
- Implicit using statements enabled (`ImplicitUsings`)
- Nullable reference types enabled (`Nullable`)
- Modern C# 10+ syntax preferred
- Property initialization with `= default!` for non-nullable reference types that will be set by ORM or constructor

**Linting:**
- No explicit linter configuration found in the project
- Code follows standard C# conventions implicitly

## Import Organization

**Order:**
1. System namespaces: `using System;`, `using System.Collections.Generic;`
2. Third-party libraries: `using Microsoft.EntityFrameworkCore;`, `using MediatR;`, `using FluentValidation;`
3. Application/Domain namespaces: `using Ticketing.Domain.Entities;`, `using Ticketing.Application.Interfaces;`
4. Project-specific namespaces: Last

**Example from `RegisterUserCommandHandler.cs`:**
```csharp
using MediatR;
using Ticketing.Application.Auth.DTOs;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Exceptions;
using Ticketing.Domain.Repositories;
```

**Path Aliases:**
- No path aliases used in the codebase
- Fully qualified namespaces throughout

## Error Handling

**Patterns:**
- Domain logic throws `DomainException` for business rule violations (located in `src/Ticketing.Domain/Exceptions/DomainException.cs`)
- Application logic throws application-specific exceptions: `NotFoundException`, `ForbiddenAccessException`, `UnauthorizedException` (located in `src/Ticketing.Application/Exceptions/`)
- Handlers check business conditions and throw appropriate exceptions rather than returning error results
- Example from `Ticket.cs`: `if (Status == TicketStatus.Closed) throw new DomainException("Cannot update a closed ticket.");`

**Exception Handling:**
- Global exception middleware in `src/Ticketing.API/Middleware/ExceptionHandlingMiddleware.cs` catches all exceptions
- Exceptions are mapped to HTTP status codes: DomainException → 400, NotFoundException → 404, ForbiddenAccessException → 403, UnauthorizedException → 401
- FluentValidation exceptions are caught and mapped to 400 with combined error messages

## Logging

**Framework:** `ILogger<T>` from Microsoft.Extensions.Logging

**Patterns:**
- Logger injected via constructor dependency injection
- Only used in middleware for unhandled exceptions: `_logger.LogError(ex, "Unhandled exception occurred.");`
- Application handlers do not include logging; exceptions are handled at middleware level

## Comments

**When to Comment:**
- Minimal inline comments; code should be self-documenting
- Domain exceptions include inline comments in method logic (see `Ticket.cs` status validation)
- No JSDoc/XML documentation observed in handlers or repositories

**JSDoc/TSDoc:**
- Not used; C# XML documentation not observed in the codebase

## Function Design

**Size:**
- Handlers are typically 15-30 lines: They validate input, coordinate domain operations, and persist
- Domain methods average 10-20 lines: Single responsibility with clear business logic
- Repository methods are 1-5 lines: Thin wrappers around EF Core operations

**Parameters:**
- Constructor parameters are injected dependencies, stored as private readonly fields
- Method parameters are explicit and named clearly: `(string title, string description, TicketPriority priority)`
- Async methods include `CancellationToken` parameter: `Handle(TRequest request, CancellationToken cancellationToken)`

**Return Values:**
- Handlers return DTOs or domain types (e.g., `AuthResponse`, `Guid` for created ticket)
- Queries return DTOs or paged responses (e.g., `PagedResponse<TicketResponse>`)
- Repository methods return entities or `null`: `Task<Ticket?>`, `Task<IEnumerable<Comment>>`
- Services return simple types: `string` (token), `bool` (verification)

## Module Design

**Exports:**
- No barrel files observed
- Each class file contains a single public type (class, record, or interface)
- Namespaces organize exports logically: `Ticketing.Domain.Entities`, `Ticketing.Application.Auth.Commands`

**Barrel Files:**
- Not used; imports reference specific files directly

## Command and Query Naming

**Commands:**
- Named as verbs in imperative form: `RegisterUserCommand`, `CreateTicketCommand`, `AssignTicketCommand`, `DeleteTicketCommand`, `ChangeTicketStatusCommand`
- Implement `IRequest<TResponse>` from MediatR
- Use record syntax for immutability: `public record RegisterUserCommand(string FullName, string Email, string Password) : IRequest<AuthResponse>;`

**Queries:**
- Named with `Get` prefix: `GetTicketByIdQuery`, `GetTicketsQuery`, `GetMyTicketsQuery`, `GetAssignedTicketsQuery`
- Implement `IRequest<TResponse>` from MediatR
- Include pagination parameters when needed: `pageNumber`, `pageSize`
- Support optional filtering parameters: `status`, `priority`, `assignedToUserId`, `createdByUserId`, `titleSearch`

## Validators

**Pattern:**
- One validator per command/query using FluentValidation
- Validators inherit from `AbstractValidator<T>`
- Rules defined in constructor using fluent API
- Common rules: `NotEmpty()`, `EmailAddress()`, `MaximumLength()`, `MinimumLength()`, `IsInEnum()`

**Example from `RegisterUserCommandValidator.cs`:**
```csharp
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200);
    }
}
```

## Domain Entity Pattern

**Encapsulation:**
- Properties are private setters: `public string Title { get; private set; }`
- Behavior methods manipulate state internally
- No parameterless constructors for public use; `private` constructors for ORM
- Collections exposed as read-only: `public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();`
- Private backing fields for collections: `private readonly List<Comment> _comments = new();`

**Aggregate Root Pattern:**
- `Ticket` is the primary aggregate root
- Contains domain logic for status transitions and assignments
- Enforces business rules through methods like `ChangeStatus()`, `AssignTo()`, `UpdateDetails()`

## Dependency Injection

**Pattern:**
- Constructor injection used throughout
- All dependencies are readonly: `private readonly IUserRepository _userRepository;`
- Extension methods register services: `services.AddApplication()`, `services.AddInfrastructure(configuration)`
- MediatR and FluentValidation automatically registered from assembly

**Example from `RegisterUserCommandHandler.cs`:**
```csharp
public RegisterUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator)
{
    _userRepository = userRepository;
    _unitOfWork = unitOfWork;
    _passwordHasher = passwordHasher;
    _jwtTokenGenerator = jwtTokenGenerator;
}
```

---

*Convention analysis: 2026-03-13*
