# Modular Monolith Backend Template (.NET 9)

Production-oriented ASP.NET Core Web API starter using **Modular Monolith**, **Clean Architecture**, **DDD**, JWT auth with refresh-token rotation, PostgreSQL, optional Redis, Docker, and xUnit tests.

> **Note:** This machine has the .NET 9 SDK. The template targets `net9.0` (current LTS-compatible SDK available here). To move to .NET 10 later, bump `TargetFramework` in `Directory.Build.props` and package versions.

## Architecture

```
Api (composition root)
 └── BuildingBlocks (Domain / Application / Infrastructure / Shared)
 └── Modules
      ├── Identity
      ├── Users
      ├── Roles
      ├── Notifications
      └── AuditLogs
           each: Domain → Application → Infrastructure + Presentation
```

**Rules**
- Modules never reference another module’s Infrastructure.
- Cross-module collaboration goes through application abstractions in BuildingBlocks or explicit Application contracts.
- Each module owns its EF Core `DbContext` and schema (`identity`, `users`, `roles`, `notifications`, `audit`).
- Controllers stay thin; business logic lives in application services.

## Features

| Module | Capabilities |
|--------|--------------|
| Identity | Register, login, logout, JWT, refresh rotation/reuse detection, forgot/reset/change password, email verification, Google-auth-ready hooks |
| Users | Profile CRUD, pagination/filter/sort, activate/deactivate, caching |
| Roles | Roles, permissions, assign roles, permission policies (`HasPermission`) |
| Notifications | In-app + email abstraction, history, mark read |
| AuditLogs | Action/IP/timestamp logging via `IAuditLogger` |

## Prerequisites

- .NET 9 SDK
- Docker Desktop (recommended) **or** local PostgreSQL 16+
- Optional: Redis 7+

## Quick start (Docker)

```bash
docker compose up --build
```

API: `http://localhost:8080`  
Swagger (Development): `http://localhost:8080/swagger`  
Health: `http://localhost:8080/health`

## Local development

1. Start Postgres (and optionally Redis):

```bash
docker compose up postgres redis -d
```

2. Configure secrets (do not commit real secrets):

Edit `src/Api/appsettings.Development.json` or use user-secrets:

```bash
dotnet user-secrets init --project src/Api/Api.csproj
dotnet user-secrets set "Jwt:Secret" "YOUR_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS" --project src/Api/Api.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=modular_monolith_dev;Username=postgres;Password=postgres" --project src/Api/Api.csproj
```

3. Run:

```bash
dotnet run --project src/Api/Api.csproj
```

On startup (Development), migrations are applied automatically and roles/permissions are seeded (`Admin`, `User`).

## Database migrations (per module)

Each module has its own DbContext and migration history. Use the helper script:

```powershell
./scripts/add-migration.ps1 -Module Identity -Name AddSomething
./scripts/add-migration.ps1 -Module Users -Name AddSomething
./scripts/add-migration.ps1 -Module Roles -Name AddSomething
./scripts/add-migration.ps1 -Module Notifications -Name AddSomething
./scripts/add-migration.ps1 -Module AuditLogs -Name AddSomething
```

Or manually:

```bash
dotnet ef migrations add <Name> \
  --project src/Modules/Identity/Infrastructure/Identity.Infrastructure.csproj \
  --startup-project src/Api/Api.csproj \
  --context IdentityDbContext \
  --output-dir Persistence/Migrations
```

**Production:** set `Database:ApplyMigrationsOnStartup` to `false` and run migrations in CI/CD or a release job before traffic switches.

```bash
dotnet ef database update --project src/Modules/Identity/Infrastructure/Identity.Infrastructure.csproj --startup-project src/Api/Api.csproj --context IdentityDbContext
# repeat for Users, Roles, Notifications, AuditLogs
```

## API overview

Base path: `/api/v1`

### Auth
- `POST /auth/register`
- `POST /auth/login`
- `POST /auth/refresh`
- `POST /auth/logout` (auth)
- `POST /auth/forgot-password`
- `POST /auth/reset-password`
- `POST /auth/verify-email`
- `POST /auth/change-password` (auth)
- `GET  /auth/google` (structure ready)

### Users
- `GET/PUT /users/me`
- `GET /users` (permission: `users.view`)
- `GET /users/{userId}`
- `POST /users/{userId}/activate|deactivate` (`users.manage`)

### Roles
- `GET /roles`, `GET /roles/permissions`
- `POST /roles`, `PUT /roles/{id}`
- `POST /roles/users/{userId}/assign`

### Notifications / Audit
- `GET/POST /notifications`, `POST /notifications/{id}/read`
- `GET /audit-logs`

All responses use a consistent envelope:

```json
{ "success": true, "data": {}, "message": null, "errors": null, "correlationId": "..." }
```

Correlation IDs flow via `X-Correlation-ID`.

## Security

- BCrypt password hashing
- Short-lived JWT access tokens + rotating refresh tokens (reuse detection revokes all sessions)
- Role + dynamic permission authorization (`[HasPermission("...")]`)
- Rate limiting (`AspNetCoreRateLimit`)
- CORS allow-list
- Global exception middleware (no stack traces in Production)
- Secrets via configuration / env / user-secrets — never hardcoded for production

## Redis

Set in config:

```json
"Cache": { "UseRedis": true, "RedisConnection": "localhost:6379" }
```

When `UseRedis` is `false`, the app uses an in-memory distributed cache (local-friendly).

## Testing

```bash
dotnet test ModularMonolith.sln
```

- `tests/UnitTests` — domain + Result + password hashing
- `tests/IntegrationTests` — WebApplicationFactory smoke tests for auth/authorization endpoints

## Deploy on Ubuntu VPS / AWS EC2 (Nginx)

1. Install Docker + Docker Compose on the host.
2. Copy the repo and set production env vars (`Jwt__Secret`, connection string, CORS origins).
3. `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build`
4. Point Nginx to `127.0.0.1:8080` using `deploy/nginx/modular-monolith.conf`.
5. Terminate TLS with Let’s Encrypt (`certbot`) in front of Nginx.

## Adding a new module

1. Create `Domain`, `Application`, `Infrastructure`, `Presentation` projects under `src/Modules/YourModule`.
2. Reference BuildingBlocks appropriately; do **not** reference other modules’ Infrastructure.
3. Add `AddYourModule(...)` DI extension and register it in `Program.cs`.
4. Add controllers via `AddApplicationPart(...)`.
5. Add a DbContext + migrations for your schema.
6. Wire permissions in `BuildingBlocks.Shared.Constants.Permissions` and seed them in Roles.

## Solution layout

```
src/
  Api/
  BuildingBlocks/
  Modules/
    Identity|Users|Roles|Notifications|AuditLogs/
tests/
  UnitTests/
  IntegrationTests/
docker-compose.yml
Dockerfile
README.md
```

## License

Use freely as a company starter template.