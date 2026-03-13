# External Integrations

**Analysis Date:** 2026-03-13

## APIs & External Services

**None Currently Integrated:**
- No external APIs or third-party services are integrated
- All features are self-contained within the application

## Data Storage

**Databases:**
- SQL Server 2022 (via EF Core SqlServer provider)
  - Connection: `ConnectionStrings.DefaultConnection`
  - Client: Entity Framework Core 10.0.3
  - DbContext: `ApplicationDbContext` at `src/Ticketing.Infrastructure/Persistence/ApplicationDbContext.cs`
  - Entities: AppUser, Ticket, Comment

**File Storage:**
- Local filesystem only - No file upload integration

**Caching:**
- None - No caching layer implemented (ASP.NET Core distributed/in-memory caching not in use)

## Authentication & Identity

**Auth Provider:**
- Custom JWT implementation
  - Implementation: Token-based authentication
  - Token Generator: `JwtTokenGenerator` at `src/Ticketing.Infrastructure/Services/JwtTokenGenerator.cs`
  - Authentication Handler: `JwtBearerDefaults.AuthenticationScheme`
  - Password Hashing: BCrypt.Net-Next (via `PasswordHasher` at `src/Ticketing.Infrastructure/Services/PasswordHasher.cs`)

**JWT Configuration:**
- Issuer: Ticketing.API
- Audience: Ticketing.Client
- Signing Algorithm: HMAC SHA-256 (SymmetricSecurityKey)
- Default Expiration: 60 minutes
- Claims included: SubjectId (user.Id), Email, Role, JTI (unique token ID)

**Authorization:**
- Role-based access control (RBAC)
- Roles: User, Agent, Admin
- Implementation: `ClaimTypes.Role` via JWT claims
- Enforce at: Controller action level via `[Authorize(Roles = "...")]`

## Monitoring & Observability

**Error Tracking:**
- None - No external error tracking service (Sentry, App Insights, etc.)
- Custom exception handling via middleware at `src/Ticketing.API/Middleware/ExceptionHandlingMiddleware.cs`

**Logs:**
- ASP.NET Core built-in logging (ILogger)
- Configuration: appsettings.json Logging section
- Default levels: Information (general), Warning (AspNetCore framework)
- Output: Console (default in development)

## CI/CD & Deployment

**Hosting:**
- Docker containerization ready
  - Base image: mcr.microsoft.com/dotnet/aspnet:10.0
  - Build image: mcr.microsoft.com/dotnet/sdk:10.0
  - Dockerfile: `/Dockerfile` (multi-stage build)
  - Port: 8080 (exposed)

**CI Pipeline:**
- None detected - No GitHub Actions, Azure Pipelines, or other CI configured

**Deployment:**
- Docker Compose orchestration available
  - File: `/docker-compose.yml`
  - Services: API (port 5000→8080) + SQL Server (port 1433)
  - Container network: implicit (via docker-compose)

## Environment Configuration

**Required env vars:**
- `ASPNETCORE_ENVIRONMENT` - Set to "Development" or "Production"
- `ConnectionStrings__DefaultConnection` - Database connection string
- `Jwt__Key` - JWT signing key (minimum 32 characters)
- `Jwt__Issuer` - JWT issuer claim value
- `Jwt__Audience` - JWT audience claim value
- `Jwt__ExpiresInMinutes` - Token lifetime in minutes

**Secrets location:**
- appsettings.json contains placeholder values
- Docker Compose: environment section (docker-compose.yml)
- Local development: user secrets via `dotnet user-secrets` recommended
- Production: Use secure configuration providers (Azure Key Vault, etc.)

## Webhooks & Callbacks

**Incoming:**
- None

**Outgoing:**
- None

## Database Details

**Provider:** SQL Server via Entity Framework Core

**Connection String Example (LocalDB):**
```
Server=(localdb)\mssqllocaldb;Database=TicketingDb_Test;Trusted_Connection=True;MultipleActiveResultSets=true
```

**Connection String Example (Docker Compose):**
```
Server=sqlserver;Database=TicketingDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;
```

**Auto-Migration:**
- Enabled on startup in `Program.cs` (line 48): `db.Database.Migrate()`
- Migrations folder: `src/Ticketing.Infrastructure/Persistence/Migrations/`

**Database Seeding:**
- Automated seeding on startup
- Seeder class: `DatabaseSeeder` at `src/Ticketing.Infrastructure/Persistence/DatabaseSeeder.cs`
- Invoked in `Program.cs` (line 51): `await seeder.SeedAsync()`

## API Documentation

**Swagger/OpenAPI:**
- Enabled by default
- Endpoint: `/swagger`
- Swagger JSON: `/swagger/v1/swagger.json`
- Library: Swashbuckle.AspNetCore 10.1.4
- Configuration: `Program.cs` lines 14-41

---

*Integration audit: 2026-03-13*
