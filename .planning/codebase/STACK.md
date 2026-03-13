# Technology Stack

**Analysis Date:** 2026-03-13

## Languages

**Primary:**
- C# - Used across all layers (Domain, Application, Infrastructure, API)

## Runtime

**Environment:**
- .NET 10.0 (LTS)
- Target Framework: net10.0

**Package Manager:**
- NuGet
- Lockfile: implicit (managed by .NET SDK)

## Frameworks

**Core:**
- ASP.NET Core 10.0 - REST API framework and HTTP server
- Entity Framework Core 10.0.3 - ORM for database persistence

**Application Architecture:**
- MediatR 14.1.0 - CQRS implementation and command/query dispatch
- FluentValidation 12.1.1 - Input validation framework
- FluentValidation.DependencyInjectionExtensions 12.1.1 - DI integration for validators

**Authentication/Security:**
- Microsoft.AspNetCore.Authentication.JwtBearer 10.0.3 - JWT bearer authentication
- BCrypt.Net-Next 4.1.0 - Password hashing

**Documentation:**
- Swashbuckle.AspNetCore 10.1.4 - Swagger/OpenAPI generation
- Microsoft.AspNetCore.OpenApi 10.0.0 - OpenAPI support

**Development/Build:**
- Microsoft.EntityFrameworkCore.Design 10.0.3 - EF Core migrations and tooling
- Microsoft.EntityFrameworkCore.Tools 10.0.3 - CLI tools for migrations

## Key Dependencies

**Critical:**
- Microsoft.EntityFrameworkCore.SqlServer 10.0.3 - SQL Server provider for EF Core (database connectivity)

**Infrastructure:**
- Microsoft.IdentityModel.Tokens - JWT token validation
- System.IdentityModel.Tokens.Jwt - JWT token creation and parsing

## Configuration

**Environment:**
- appsettings.json - Production settings
- appsettings.Development.json - Development overrides
- Environment variables via ASPNETCORE_ENVIRONMENT
- Configuration sections: ConnectionStrings, Jwt, Logging

**Build:**
- Target framework: net10.0
- Nullable reference types enabled across all projects
- Implicit using statements enabled (top-level imports)

## Platform Requirements

**Development:**
- .NET 10 SDK
- SQL Server (LocalDB or full instance)
- Optional: Docker and Docker Compose

**Production:**
- .NET 10 Runtime (ASP.NET Core)
- SQL Server database
- Container runtime (Docker) recommended

## Project Structure

```
Ticketing.sln
├── src/
│   ├── Ticketing.Domain/           # Core business logic (no external dependencies)
│   ├── Ticketing.Application/      # CQRS commands/queries, DTOs, validators
│   ├── Ticketing.Infrastructure/   # EF Core, repositories, services
│   └── Ticketing.API/              # ASP.NET Core controllers, middleware, configuration
└── tests/
    └── Ticketing.Tests/            # Unit and integration tests
```

## Dependency Graph

```
Ticketing.API
├── Ticketing.Application
│   └── Ticketing.Domain
└── Ticketing.Infrastructure
    └── Ticketing.Application
        └── Ticketing.Domain
```

---

*Stack analysis: 2026-03-13*
