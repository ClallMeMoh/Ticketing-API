---
phase: 01-domain-and-persistence-foundation
plan: 01
subsystem: database
tags: [efcore, domain, cqrs, concurrency, row-version, audit]

# Dependency graph
requires: []
provides:
  - AgentProfile entity extending AuditableEntity with EF Core configuration
  - TicketAssignmentHistory EF Core configuration with FK and index constraints
  - RowVersion concurrency token configured on Ticket via .IsRowVersion()
  - ICurrentUserService.Email property for audit trail support
  - ApplicationDbContext DbSets for AgentProfile and TicketAssignmentHistory
  - Domain tests covering new Ticket status transitions and AgentProfile behavior
affects:
  - 01-02 handler wiring and repository implementations
  - 01-03 migration generation (depends on DbContext DbSets and configurations)
  - Phase 2 auto-assignment algorithm (depends on RowVersion concurrency and AgentProfile)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "AgentProfile and AuditableEntity entities pick up CreatedAt/UpdatedAt automatically via DbContext.SaveChangesAsync"
    - "EF Core concurrency token via .IsRowVersion() on byte[] RowVersion property"
    - "One-to-one FK configured in EF Core via HasOne().WithOne().HasForeignKey<T>() pattern"

key-files:
  created:
    - src/Ticketing.Infrastructure/Persistence/Configurations/AgentProfileConfiguration.cs
    - src/Ticketing.Infrastructure/Persistence/Configurations/TicketAssignmentHistoryConfiguration.cs
    - tests/Ticketing.Tests/Domain/AgentProfileTests.cs
  modified:
    - src/Ticketing.Domain/Entities/AgentProfile.cs
    - src/Ticketing.Application/Interfaces/ICurrentUserService.cs
    - src/Ticketing.Infrastructure/Services/CurrentUserService.cs
    - src/Ticketing.Infrastructure/Persistence/Configurations/TicketConfiguration.cs
    - src/Ticketing.Infrastructure/Persistence/ApplicationDbContext.cs
    - tests/Ticketing.Tests/Domain/TicketTests.cs

key-decisions:
  - "AgentProfile extends AuditableEntity so EF Core audit interceptor populates CreatedAt/UpdatedAt automatically"
  - "RowVersion .IsRowVersion() fix applied before any handler code — without it concurrency protection was a silent no-op"
  - "Email on ICurrentUserService returns string? (nullable) not string.Empty fallback, since absence of email is meaningful"

patterns-established:
  - "EF configuration pattern: HasOne/WithMany/HasForeignKey/OnDelete per relationship"
  - "Concurrency token: byte[] RowVersion + .IsRowVersion() in EF config"

requirements-completed: [DOM-01, DOM-02, DOM-03, DOM-04, DOM-06]

# Metrics
duration: 2min
completed: 2026-03-13
---

# Phase 1 Plan 01: Domain Fixes, EF Configurations, and DbContext Updates Summary

**EF Core configurations for AgentProfile and TicketAssignmentHistory, RowVersion concurrency token fix, Email on ICurrentUserService, and 23 passing domain tests covering status transitions and agent profile behavior**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-13T19:02:42Z
- **Completed:** 2026-03-13T19:04:46Z
- **Tasks:** 2
- **Files modified:** 9

## Accomplishments
- Fixed AgentProfile to extend AuditableEntity — now picks up CreatedAt/UpdatedAt automatically
- Added .IsRowVersion() to TicketConfiguration, activating the concurrency token that was a silent no-op before
- Created AgentProfileConfiguration and TicketAssignmentHistoryConfiguration with correct FK constraints and indexes
- Added AgentProfiles and AssignmentHistories DbSets to ApplicationDbContext
- Added Email property to ICurrentUserService and CurrentUserService (reads ClaimTypes.Email)
- 23 domain tests all pass: 3 new Ticket transition tests + 8 new AgentProfile tests

## Task Commits

Each task was committed atomically:

1. **Task 1: Domain fixes, EF configurations, and DbContext updates** - `c3c9bc5` (feat)
2. **Task 2: Unit tests for domain behavior and new status transitions** - `258bd85` (test)

## Files Created/Modified
- `src/Ticketing.Domain/Entities/AgentProfile.cs` - Changed base class from BaseEntity to AuditableEntity
- `src/Ticketing.Application/Interfaces/ICurrentUserService.cs` - Added string? Email property
- `src/Ticketing.Infrastructure/Services/CurrentUserService.cs` - Added Email reading ClaimTypes.Email
- `src/Ticketing.Infrastructure/Persistence/Configurations/TicketConfiguration.cs` - Added .IsRowVersion() for RowVersion
- `src/Ticketing.Infrastructure/Persistence/Configurations/AgentProfileConfiguration.cs` - New: unique index on UserId, cascade FK to AppUser
- `src/Ticketing.Infrastructure/Persistence/Configurations/TicketAssignmentHistoryConfiguration.cs` - New: index on TicketId, cascade/restrict FKs, Reason max 500
- `src/Ticketing.Infrastructure/Persistence/ApplicationDbContext.cs` - Added AgentProfiles and AssignmentHistories DbSets
- `tests/Ticketing.Tests/Domain/TicketTests.cs` - Added 3 new status transition tests
- `tests/Ticketing.Tests/Domain/AgentProfileTests.cs` - New: 8 AgentProfile unit tests

## Decisions Made
- Email on ICurrentUserService is `string?` (not `string`) — absence of email claim is semantically different from empty string
- RowVersion fix applied now as a prerequisite for Phase 2 concurrency-safe auto-assignment
- AgentProfile one-to-one FK uses Cascade delete — deleting an AppUser removes their agent profile

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- EF configurations are complete and consistent — Plan 02 can implement repository classes and handler wiring
- DbContext has all DbSets needed for migration generation in Plan 03
- ICurrentUserService.Email is available for any handler that needs to audit by email
- All 23 domain tests passing — no regressions from changes

---
*Phase: 01-domain-and-persistence-foundation*
*Completed: 2026-03-13*
