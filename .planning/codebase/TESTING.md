# Testing Patterns

**Analysis Date:** 2026-03-13

## Test Framework

**Runner:**
- xUnit 2.9.3
- Config: `tests/Ticketing.Tests/Ticketing.Tests.csproj`

**Assertion Library:**
- xUnit assertions (built-in `Assert` class)

**Mocking Library:**
- NSubstitute 5.3.0

**Run Commands:**
```bash
dotnet test                    # Run all tests
dotnet test --watch           # Watch mode
dotnet test /p:CollectCoverageMetrics=true  # Coverage (via coverlet.collector)
```

## Test File Organization

**Location:**
- Tests are co-located in separate `tests/Ticketing.Tests` project
- Mirrors domain structure with `Application` and `Domain` subdirectories

**Naming:**
- Test files: `{ClassUnderTest}Tests.cs`
- Examples: `TicketTests.cs`, `CreateTicketCommandHandlerTests.cs`, `AssignTicketCommandHandlerTests.cs`

**Structure:**
```
tests/Ticketing.Tests/
├── Application/
│   ├── CreateTicketCommandHandlerTests.cs
│   ├── CreateTicketCommandValidatorTests.cs
│   ├── DeleteTicketCommandHandlerTests.cs
│   └── AssignTicketCommandHandlerTests.cs
└── Domain/
    └── TicketTests.cs
```

## Test Structure

**Suite Organization:**
```csharp
public class TicketTests
{
    private Ticket CreateTicket() =>
        new("Test Ticket", "Description", TicketPriority.Medium, Guid.NewGuid());

    [Fact]
    public void Constructor_SetsStatusToOpen()
    {
        var ticket = CreateTicket();
        Assert.Equal(TicketStatus.Open, ticket.Status);
    }
}
```

**Patterns:**
- One public test class per unit under test
- Private helper factory methods for creating test objects: `CreateTicket()`
- Substitutes (mocks) created as class fields when testing handlers
- Test methods use `[Fact]` attribute for simple tests, `[Theory]` with `[InlineData]` for parameterized tests
- Test method naming: `{MethodName}_{Scenario}_{ExpectedResult}`
  - Examples: `UpdateDetails_WhenOpen_UpdatesFields()`, `AssignTo_WhenOpen_SetsAssigned()`, `ChangeStatus_WhenClosed_OnlyAllowsReopen()`

## Mocking

**Framework:** NSubstitute

**Patterns:**
```csharp
private readonly ITicketRepository _ticketRepository = Substitute.For<ITicketRepository>();
private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

// Setup return value
_currentUser.UserId.Returns(Guid.NewGuid());
_currentUser.Role.Returns(nameof(UserRole.Admin));

// Verify calls
await _ticketRepository.Received(1).AddAsync(Arg.Is<Ticket>(t =>
    t.Title == "Test" && t.Description == "Description"));

await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

_ticketRepository.Received(1).Delete(ticket);
```

**What to Mock:**
- Repository interfaces (`ITicketRepository`, `IUserRepository`)
- Unit of work: `IUnitOfWork`
- Services (`ICurrentUserService`, `IJwtTokenGenerator`, `IPasswordHasher`)
- Read services (`ITicketReadService`, `ICommentReadService`)

**What NOT to Mock:**
- Domain entities are created directly, not mocked
- Domain exceptions are tested directly
- Validators are tested with real instances
- FluentValidation behavior is tested directly

## Fixtures and Factories

**Test Data:**
```csharp
private Ticket CreateTicket() =>
    new("Test Ticket", "Description", TicketPriority.Medium, Guid.NewGuid());

public async Task Handle_CreatesTicketAndSaves()
{
    var command = new CreateTicketCommand("Test", "Description", TicketPriority.High);
    // ...
}
```

**Location:**
- Factory methods are private methods in test classes
- No separate fixture files; test data is inline or created via factories
- Guid.NewGuid() used for generating unique IDs in tests

## Coverage

**Requirements:** Not enforced

**View Coverage:**
```bash
dotnet test /p:CollectCoverageMetrics=true
```

Coverage measured via `coverlet.collector` 6.0.4.

## Test Types

**Unit Tests:**
- Domain entity behavior: `TicketTests.cs` tests all state transitions and business rules
- Handler logic: Tests in `Application/` directory test command/query handlers with mocked dependencies
- Validator logic: `CreateTicketCommandValidatorTests.cs` tests validation rules with valid/invalid data
- Scope: Single class in isolation; dependencies are mocked

**Integration Tests:**
- Not present in current test suite
- No database integration tests

**E2E Tests:**
- Not used

## Common Patterns

**Fact vs Theory:**
- `[Fact]` for single scenario tests
- `[Theory]` with `[InlineData]` for parameterized tests

**Example of Theory pattern from `CreateTicketCommandValidatorTests.cs`:**
```csharp
[Theory]
[InlineData("", "Description")]
[InlineData("Title", "")]
[InlineData("", "")]
public void Validate_EmptyFields_IsInvalid(string title, string description)
{
    var command = new CreateTicketCommand(title, description, TicketPriority.Low);
    var result = _validator.Validate(command);
    Assert.False(result.IsValid);
}
```

**Exception Testing:**
```csharp
[Fact]
public void UpdateDetails_WhenClosed_ThrowsDomainException()
{
    var ticket = CreateTicket();
    ticket.Close();

    Assert.Throws<DomainException>(() =>
        ticket.UpdateDetails("Title", "Desc", TicketPriority.Low));
}

[Fact]
public async Task Handle_WhenNotAdmin_ThrowsForbidden()
{
    _currentUser.Role.Returns(nameof(UserRole.User));
    var command = new DeleteTicketCommand(Guid.NewGuid());

    await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
        _handler.Handle(command, CancellationToken.None));
}
```

**Async Testing:**
```csharp
public async Task Handle_CreatesTicketAndSaves()
{
    var command = new CreateTicketCommand("Test", "Description", TicketPriority.High);

    await _handler.Handle(command, CancellationToken.None);

    await _ticketRepository.Received(1).AddAsync(Arg.Is<Ticket>(t =>
        t.Title == "Test" &&
        t.Description == "Description" &&
        t.Priority == TicketPriority.High));
}
```

## Test Coverage Status

**Covered Areas:**
- Domain entity state transitions (Ticket aggregate)
- Handler authorization checks
- Handler business logic with mocked dependencies
- Validator field validation
- Validator enum validation

**Not Covered:**
- Query handlers
- Integration with EF Core
- Authentication/authorization integration
- Comments functionality (limited tests)
- Complete end-to-end flows

## Test Best Practices Observed

1. **Arrange-Act-Assert:** Tests follow AAA pattern consistently
2. **Single Responsibility:** Each test verifies one behavior
3. **Descriptive Names:** Test names clearly describe what is tested and expected outcome
4. **Proper Cleanup:** NSubstitute mocks are disposed between tests (by default)
5. **Verification:** Handlers verify that repository methods were called the expected number of times
6. **Exception Testing:** Both sync and async exception scenarios tested

---

*Testing analysis: 2026-03-13*
