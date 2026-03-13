# Codebase Concerns

**Analysis Date:** 2026-03-13

## Security Considerations

**Inconsistent Role Authorization Comparisons:**
- Risk: String-based role comparison using `nameof(UserRole.Admin)` versus enum-based comparison creates inconsistency and potential bypasses
- Files: `src/Ticketing.Application/Tickets/Commands/AssignTicket/AssignTicketCommandHandler.cs` (line 30), `src/Ticketing.Application/Tickets/Commands/DeleteTicket/DeleteTicketCommandHandler.cs` (line 27), `src/Ticketing.Application/Tickets/Commands/ChangeTicketStatus/ChangeTicketStatusCommandHandler.cs` (line 31), `src/Ticketing.Application/Tickets/Commands/UpdateTicket/UpdateTicketCommandHandler.cs` (line 31)
- Current mitigation: Some handlers use string comparison while others use enum comparison
- Recommendations: Establish a centralized authorization check method that always compares using enums, not string names. Create an extension method like `IsAdmin()` and `IsAgentOrAdmin()` on the user role to eliminate string-based checks

**Duplicate Authorization Logic:**
- Risk: Authorization checks are repeated in multiple command handlers, creating maintenance burden and inconsistency
- Files: `src/Ticketing.Application/Tickets/Commands/UpdateTicket/UpdateTicketCommandHandler.cs`, `src/Ticketing.Application/Tickets/Commands/ChangeTicketStatus/ChangeTicketStatusCommandHandler.cs`, `src/Ticketing.Application/Tickets/Commands/AssignTicket/AssignTicketCommandHandler.cs`, `src/Ticketing.Application/Tickets/Commands/DeleteTicket/DeleteTicketCommandHandler.cs`
- Current mitigation: Each handler implements its own role-based authorization
- Recommendations: Create an authorization service or pipeline behavior that extracts common authorization patterns. Alternatively, use attributes like `[Authorize(Roles = "Admin,Agent")]` more consistently at the controller level combined with command-level validation

**Missing Authorization on Ticket Read Endpoints:**
- Risk: `GetTicketById` and `GetTickets` queries have no authorization checks to verify users should access specific tickets
- Files: `src/Ticketing.Application/Tickets/Queries/GetTicketById/GetTicketByIdQueryHandler.cs`, `src/Ticketing.Application/Tickets/Queries/GetTickets/GetTicketsQueryHandler.cs`
- Current mitigation: Queries return tickets without permission validation
- Recommendations: Add authorization checks to query handlers to ensure users can only view tickets they created, are assigned to, or have admin role. Implement a cross-cutting concern or add explicit checks in each query handler

**Comments Authorization Not Enforced:**
- Risk: No authorization verification for adding or viewing comments - any authenticated user might be able to comment on any ticket
- Files: `src/Ticketing.Application/Comments/Commands/AddComment/AddCommentCommandHandler.cs`
- Current mitigation: Only checks if ticket and user exist
- Recommendations: Add authorization to verify user can access the ticket before allowing comment creation

## Test Coverage Gaps

**No Unit or Integration Tests:**
- What's not tested: All application logic, command handlers, query handlers, domain logic validation, authorization rules, error scenarios
- Files: Entire `src/Ticketing.Application/` and `src/Ticketing.Domain/` directories
- Risk: Logic changes may silently introduce bugs, authorization bypasses could go undetected, domain rules may be violated
- Priority: High

**Missing Permission Validation Tests:**
- What's not tested: Scenarios where users without proper roles attempt operations (UpdateTicket as non-owner/non-agent, AssignTicket as regular user, DeleteTicket as non-admin)
- Files: Command handlers in `src/Ticketing.Application/Tickets/Commands/`
- Risk: Authorization vulnerabilities could exist and be missed during development
- Priority: High

**No Database Integration Tests:**
- What's not tested: Entity Framework Core mapping, audit field updates, concurrency handling with RowVersion, complex queries with multiple filters
- Files: `src/Ticketing.Infrastructure/Persistence/` and repository implementations
- Risk: Data persistence bugs could cause data loss or corruption in production
- Priority: Medium

## Fragile Areas

**Domain Authorization Logic Scattered Across Handlers:**
- Files: Multiple command handlers checking `_currentUser.Role`
- Why fragile: Same authorization pattern duplicated in 4+ places means one change point becomes many, and inconsistency is likely
- Safe modification: Extract a shared `IAuthorizationService` with methods like `CanDeleteTicket(userId, role)` to centralize authorization
- Test coverage: No tests verify authorization behavior

**Ticket Status State Machine Not Fully Enforced:**
- Files: `src/Ticketing.Domain/Entities/Ticket.cs`
- Why fragile: Allows certain transitions (e.g., Closed → Open via Reopen, but handlers also allow direct status change to Open without validation). The `ChangeStatus` method and `Reopen` method could allow inconsistent state transitions
- Safe modification: Review the complete set of allowed state transitions. Either enforce them in one place or document clearly which methods enforce which rules
- Test coverage: No tests validate all valid/invalid state transitions

**Role Comparison String vs Enum Inconsistency:**
- Files: Command handlers across `src/Ticketing.Application/Tickets/Commands/`
- Why fragile: Some use `nameof(UserRole.Admin)` string comparison, others use enum pattern matching. This creates confusion about which is correct and could lead to typos
- Safe modification: Standardize on one approach: create extension methods on the enum for role checks
- Test coverage: No tests validate role authorization

## Performance Bottlenecks

**N+1 Query Risk in Ticket Listings:**
- Problem: `TicketReadService.GetPagedAsync()` uses Select projection but still fetches `CreatedByUser` and `AssignedToUser` relationships in the select clause
- Files: `src/Ticketing.Infrastructure/Repositories/TicketReadService.cs` (lines 71-82)
- Cause: The query uses `.Select()` with navigation properties but doesn't explicitly load them in the initial query. EF Core may create separate queries for user names
- Improvement path: Verify that `.Select()` with navigation property access is properly compiled to JOINs. Use `.Include()` or `.ThenInclude()` if projections don't automatically include related entities

**Potential Issue with Ticket.Comments Access:**
- Problem: `GetByIdWithCommentsAsync` returns full Ticket with Comments collection, but queries may not properly eager-load
- Files: `src/Ticketing.Infrastructure/Repositories/TicketRepository.cs` (line 20-23)
- Cause: Comments collection may trigger separate queries if accessed outside the DbContext scope
- Improvement path: Verify that `.Include(t => t.Comments)` is sufficient, or consider using explicit `.ThenInclude()` for user data in comments

## Tech Debt

**Unused Repository Interfaces:**
- Issue: `IAgentProfileRepository` and `IAssignmentHistoryRepository` are defined but not implemented or used
- Files: `src/Ticketing.Domain/Repositories/IAgentProfileRepository.cs`, `src/Ticketing.Domain/Repositories/IAssignmentHistoryRepository.cs`
- Impact: Dead code in the repository contract layer that adds confusion about project scope
- Fix approach: Either implement these repositories and use them in handlers, or remove the interfaces entirely. If removed, verify that `AgentProfile` and `TicketAssignmentHistory` entities are also not needed

**Inconsistent Repository Patterns:**
- Issue: Some repositories (like `TicketRepository`) have minimal methods, while business logic is split between repositories and read services
- Files: `src/Ticketing.Infrastructure/Repositories/TicketRepository.cs` vs `src/Ticketing.Infrastructure/Repositories/TicketReadService.cs`
- Impact: Unclear responsibility - is TicketRepository for writes and TicketReadService for reads? Pattern not consistently applied
- Fix approach: Document the CQRS pattern being used (write repository vs read service) or consolidate if the split adds no value

**Missing Centralized Authorization Service:**
- Issue: Authorization checks are embedded in handlers rather than extracted to a service
- Files: `src/Ticketing.Application/Tickets/Commands/` handlers
- Impact: Authorization logic is hard to test, maintain, and audit
- Fix approach: Create `IAuthorizationService` with methods for each permission check, inject into handlers, and wire through dependency injection

**No Exception Logging for Debuggability:**
- Issue: Exception handler in middleware logs exception but only returns generic error messages to client (by design)
- Files: `src/Ticketing.API/Middleware/ExceptionHandlingMiddleware.cs` (line 27)
- Impact: In development, unhandled exceptions are logged but application diagnostics are difficult
- Fix approach: Ensure logging is properly configured in Program.cs and that all exceptions include context (e.g., request ID) for correlation

## Missing Critical Features

**No Audit Trail for Ticket Changes:**
- Problem: Tickets track `CreatedAt` and `UpdatedAt` but don't track who updated or what changed
- Blocks: Cannot audit who made what changes to tickets, history is lost
- Impact: Important for compliance and debugging customer issues

**No Soft Deletes:**
- Problem: Deleting tickets permanently removes data
- Blocks: Cannot recover deleted tickets, cannot maintain referential integrity if comments or history existed
- Impact: Users can accidentally delete important data with no recovery option

**No Bulk Operations:**
- Problem: System can only operate on one ticket at a time
- Blocks: Agents cannot bulk assign tickets or bulk change status
- Impact: Administrative efficiency is reduced

## Known Bugs

**Potential String-based Role Check Bypasses:**
- Symptoms: If role string comparison is used anywhere and there's a typo (e.g., `"admin"` vs `"Admin"`), authorization could fail silently
- Files: `src/Ticketing.Application/Tickets/Commands/AssignTicket/AssignTicketCommandHandler.cs` (line 30), `src/Ticketing.Application/Tickets/Commands/DeleteTicket/DeleteTicketCommandHandler.cs` (line 27)
- Trigger: Inspect handlers to see if role values match between JWT token generation and handler comparisons
- Workaround: Use enum comparisons exclusively or add tests that verify role strings match

**CurrentUserService May Return Empty Guid:**
- Symptoms: `ICurrentUserService.UserId` returns `Guid.Empty` if JWT token is missing `sub` claim
- Files: `src/Ticketing.Infrastructure/Services/CurrentUserService.cs` (lines 16-21)
- Trigger: When authentication is bypassed or token is malformed
- Workaround: Check for `Guid.Empty` in handlers or throw exception earlier in middleware

## Scaling Limits

**Database Indexes:**
- Current capacity: System depends on EF Core's default indexing (primary key only)
- Limit: `GetTickets` queries with filters on `Status`, `Priority`, `AssignedToUserId`, `CreatedByUserId` will do full table scans as tables grow
- Scaling path: Add database indexes on commonly filtered columns: `Status`, `Priority`, `AssignedToUserId`, `CreatedByUserId`, and `CreatedAt` (for sorting)

**No Pagination Validation:**
- Current capacity: Clients can request any page size up to the default
- Limit: Client could request pageSize = 1,000,000 causing memory and query issues
- Scaling path: Add validation to `PagedResponse` queries to enforce maximum page size (e.g., max 100 items per page)

**No Query Result Limits:**
- Current capacity: `GetTickets` query can return up to pageSize items (default 10, but configurable by client)
- Limit: No absolute limit on result set memory
- Scaling path: Document and enforce page size limits in query handlers or add a global policy

## Dependencies at Risk

**No Semantic Versioning Lock:**
- Risk: `Ticketing.Application/Ticketing.Application.csproj` likely has dependency versions that can auto-update to breaking changes
- Impact: Builds could break silently with dependency updates
- Migration plan: Pin exact versions in csproj or add integration tests that would catch breaking changes

---

*Concerns audit: 2026-03-13*
