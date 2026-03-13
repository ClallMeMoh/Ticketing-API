# Development Problem Log

## Issue: IQueryable leaking EF Core into the Application layer
- **Cause:** Repository interfaces exposed `IQueryable<T>`, forcing the Application layer to reference `Microsoft.EntityFrameworkCore` for `ToListAsync()` and `CountAsync()`. This broke the clean architecture boundary by leaking infrastructure behavior into the Application layer.
- **Fix:** Replaced `IQueryable<T> Query()` with `Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(...)` methods on the repository interfaces. Moved all EF Core query logic (filtering, sorting, pagination) into the repository implementations. Removed the `Microsoft.EntityFrameworkCore` package reference from the Application project.
- **Prevention:** Repository interfaces should return materialized collections, not queryables. Keep EF Core concerns in the Infrastructure layer.

## Issue: OpenApi v2 breaking changes with Swashbuckle 10.x
- **Cause:** Swashbuckle 10.x depends on `Microsoft.OpenApi` v2, which moved all types from `Microsoft.OpenApi.Models` to `Microsoft.OpenApi`. Additionally, `OpenApiReference` was replaced by type-specific reference classes (e.g., `OpenApiSecuritySchemeReference`), and `AddSecurityRequirement` now takes a `Func<OpenApiDocument, OpenApiSecurityRequirement>`.
- **Fix:** Updated the namespace, used `OpenApiSecuritySchemeReference("Bearer", doc)` instead of the old `OpenApiReference` pattern, and changed `Array.Empty<string>()` to `new List<string>()`.
- **Prevention:** When using newer Swashbuckle versions, check the OpenApi library version for API changes. Tutorials written for OpenApi v1 will not compile against v2.

## Issue: EF Core Design package missing for migrations
- **Cause:** Running `dotnet ef migrations add` failed because `Microsoft.EntityFrameworkCore.Design` was not referenced in the startup project.
- **Fix:** Added the package to the API project.
- **Prevention:** Always include the Design package in the startup project when using EF Core migrations.

## Issue: ApplicationException used for all handler errors (incorrect HTTP semantics)
- **Cause:** All handler errors threw generic `ApplicationException`, which mapped to 400 Bad Request regardless of whether the error was a not-found (should be 404) or a permission denial (should be 403).
- **Fix:** Created `NotFoundException` and `ForbiddenAccessException` in the Application layer. Updated all handlers to throw the correct exception type. Updated `ExceptionHandlingMiddleware` to map `NotFoundException` to 404, `ForbiddenAccessException` to 403, and `UnauthorizedAccessException` to 401. Removed `ApplicationException` catch entirely.
- **Prevention:** Use semantically correct exception types from the start. Match exception types to HTTP status codes in the middleware.

## Issue: Inconsistent fallback values in mapping extensions
- **Cause:** `TicketMappingExtensions.ToResponse()` used `string.Empty` for missing user names, while `CommentMappingExtensions.ToResponse()` used `"Unknown"`.
- **Fix:** Standardized both to use `string.Empty`.
- **Prevention:** When adding mapping logic, check existing mappers for conventions.

## Issue: No admin user exists, blocking ticket assignment
- **Cause:** `RegisterUserCommand` always creates users with `UserRole.User`. Since `AssignTicketCommandHandler` requires Admin or Agent role, no user could ever assign tickets.
- **Fix:** Created `DatabaseSeeder` in the Infrastructure layer that seeds a default admin user (`admin@test.com` / `Admin123!`) on startup if no admin exists. Registered it in DI and called it from `Program.cs` after migrations.
- **Prevention:** When implementing role-based features, ensure there is a way to bootstrap privileged users.

## Issue: Admin seeder skips even when seed credentials are stale
- **Cause:** The original seeder checked `AnyAsync(u => u.Role == UserRole.Admin)`. If an admin existed from a prior run with a different password hash (e.g. DB survived a code change), the seeder would skip, and login with the expected seed credentials would fail with "Invalid email or password."
- **Fix:** Changed the seeder to check by the specific seed email (`admin@test.com`) instead of by role. If the admin exists but the password hash doesn't match the expected seed password, it resets the hash. Added `ResetPassword(string newPasswordHash)` method to `AppUser`.
- **Prevention:** Seed data checks should match on identity (email), not on role. Always handle the "exists but stale" case for seed users.

## Issue: Auth handlers still using ApplicationException after exception refactor
- **Cause:** `LoginUserCommandHandler` and `RegisterUserCommandHandler` were missed when replacing `ApplicationException` with specific types. Since the middleware no longer catches `ApplicationException`, these would bubble up as 500 Internal Server Error.
- **Fix:** Changed both to throw `DomainException` (invalid credentials / duplicate email are business rule violations, correctly mapped to 400 Bad Request).
- **Prevention:** After a cross-cutting refactor, grep the entire `src/` directory for the old type to ensure zero remaining usages.

## Issue: Guid.NewGuid() causes SQL Server index fragmentation
- **Cause:** `BaseEntity` generated IDs with `Guid.NewGuid()`, which produces random GUIDs. SQL Server clustered indexes on random GUIDs cause heavy page splits and fragmentation on inserts.
- **Fix:** Removed `Guid.NewGuid()` from `BaseEntity.Id` and configured EF Core to use `NEWSEQUENTIALID()` as the SQL Server default value for all entity IDs via `OnModelCreating`.
- **Prevention:** When using GUIDs as primary keys on SQL Server, always use sequential GUIDs (`NEWSEQUENTIALID()` or `SequentialGuidValueGenerator`).

## Issue: DateTime.UtcNow not testable in audit fields
- **Cause:** `ApplicationDbContext.SetAuditFields()` called `DateTime.UtcNow` directly, making it impossible to mock time in unit tests.
- **Fix:** Injected `TimeProvider` (built-in since .NET 8) into `ApplicationDbContext` and used `_timeProvider.GetUtcNow().UtcDateTime` instead of `DateTime.UtcNow`. Registered `TimeProvider.System` in the DI container.
- **Prevention:** Never use `DateTime.UtcNow` directly in production code. Use `TimeProvider` for testability.

## Issue: Invalid credentials mapped to 400 Bad Request instead of 401 Unauthorized
- **Cause:** `LoginUserCommandHandler` threw `DomainException` for invalid credentials, which the middleware mapped to 400 Bad Request. HTTP semantics require 401 for authentication failures.
- **Fix:** Created `UnauthorizedException` in the Application layer. Changed `LoginUserCommandHandler` to throw `UnauthorizedException`. Added it to the middleware mapping before `DomainException` (401 Unauthorized).
- **Prevention:** Distinguish between business rule violations (400) and authentication failures (401). Use dedicated exception types for each HTTP semantic.

## Issue: No controller-level role checks, wasting resources on unauthorized requests
- **Cause:** All controller actions only had `[Authorize]` at the class level. Role checks were done inside handlers after model binding, DB queries, and handler execution. Unauthorized users consumed server resources before being rejected.
- **Fix:** Added `[Authorize(Roles = "Admin,Agent")]` to the Assign action and `[Authorize(Roles = "Admin")]` to the Delete action on `TicketsController`. Handler-level checks remain as defense-in-depth.
- **Prevention:** Apply role-based `[Authorize]` attributes at the controller/action level for fast-fail authorization. Keep handler-level checks as a secondary guard.

## Issue: Read queries materializing full domain entities with Include() instead of projecting to DTOs
- **Cause:** Query handlers fetched full `Ticket` and `Comment` entities with `.Include()` for navigation properties, then mapped them to DTOs in memory. This loaded unnecessary columns and materialized full entity graphs on read paths.
- **Fix:** Created `ITicketReadService` and `ICommentReadService` interfaces in the Application layer with implementations in Infrastructure that use `.Select()` to project directly to DTOs at the database level. Removed paged read methods from write-side repository interfaces. Deleted unused mapping extension files (`TicketMappingExtensions`, `CommentMappingExtensions`). Also removed unnecessary `Include(CreatedByUser/AssignedToUser)` from write-side `TicketRepository.GetByIdAsync()` since command handlers only need the entity for mutations.
- **Prevention:** On read paths, use `.Select()` projections to return only the fields needed. Separate read and write concerns: repositories handle domain entity persistence, read services handle DTO projections.

## Issue: MassTransit v9 license requirement blocked runtime startup
- **Cause:** `MassTransit.RabbitMQ` v9 requires a runtime license key (`MT_LICENSE`/`MT_LICENSE_PATH`). Local/dev environment had no license configured, causing API host startup failure.
- **Fix:** Pinned MassTransit packages to v8.5.1 for API/Worker runtime compatibility in this project.
- **Prevention:** Verify runtime licensing requirements before package major-version upgrades, especially for messaging/infrastructure dependencies.

## Issue: RabbitMQ host port collision prevented compose startup
- **Cause:** Host port `5672` was already allocated by another local container stack.
- **Fix:** Remapped project RabbitMQ ports to `5673` (AMQP) and `15673` (management UI) in `docker-compose.yml`.
- **Prevention:** Reserve per-project port ranges or check occupied ports before finalizing compose mappings.

## Issue: Worker container failed because required services were missing
- **Cause:** Worker DI path did not register some Application-level dependencies (`ICurrentUserService`, `IPasswordHasher`, `IJwtTokenGenerator`), and worker runtime image initially lacked required ASP.NET shared framework.
- **Fix:** Switched worker base image to `mcr.microsoft.com/dotnet/aspnet:10.0` and added worker-safe service implementations/registrations.
- **Prevention:** When reusing Application handlers in non-API hosts, validate startup with `ValidateOnBuild` mindset and provide host-specific infrastructure stubs/adapters.

## Issue: Ticket-created event published to wrong topology
- **Cause:** API publisher sent directly to a custom exchange URI while worker consumed default message topology queue, so events were not delivered to consumer.
- **Fix:** Changed publisher to use `IPublishEndpoint.Publish(TicketCreatedEvent)` so MassTransit topology wiring matches consumer bindings.
- **Prevention:** For event-style communication, prefer `Publish` over direct `Send` unless queue/exchange ownership is explicitly managed end-to-end.

## Issue: EF Core could not translate active-load projection query
- **Cause:** Complex projection in `GetAssignableAgentsWithActiveLoadAsync()` generated a LINQ expression EF Core could not translate.
- **Fix:** Reworked to two-step query/materialization: fetch candidate agents, fetch active tickets for those agents, aggregate weighted load in memory, then map snapshots.
- **Prevention:** Keep EF projections translation-friendly; split query and aggregation when expression complexity grows.

## Issue: Swagger UI showed "Unable to render this definition" in Docker dev flow
- **Cause:** HTTPS redirection in development container led to inconsistent UI behavior while service was exposed over HTTP only.
- **Fix:** Applied `UseHttpsRedirection()` only outside Development and verified `/swagger/v1/swagger.json` returns valid `openapi: "3.0.4"`.
- **Prevention:** In containerized local development, align protocol behavior with actual exposed endpoints and avoid forced HTTPS unless certificates/ports are configured.
