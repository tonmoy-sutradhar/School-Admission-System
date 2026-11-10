# Full Code Structure

Modular Monolith (.NET 9) — পুরো প্রজেক্টের ফোল্ডার ও ফাইল ম্যাপ।  
নতুন ডেভেলপার দ্রুত বুঝতে পারবে কোথায় কী আছে।

> বিস্তারিত আর্কিটেকচার গাইড (Bangla): [`docs/FOLDER-STRUCTURE-BANGLA.md`](docs/FOLDER-STRUCTURE-BANGLA.md)  
> সেটআপ ও রান গাইড: [`README.md`](README.md)

---

## এক নজরে

```
Api  →  Modules (Identity, Users, Roles, Notifications, AuditLogs)
     →  BuildingBlocks (শেয়ারড)
```

প্রতিটি Module-এ **৪ লেয়ার**:

| Layer | কাজ |
|--------|------|
| **Domain** | Entity, domain event, ব্যবসার নিয়ম |
| **Application** | Service, DTO, Validator, interface |
| **Infrastructure** | EF Core, DB, JWT, external services |
| **Presentation** | HTTP Controller |

---

## সম্পূর্ণ ফাইল ট্রি

```
Tonmoy Backend Templet C/
├── CODE-STRUCTURE.md              ← এই ফাইল
├── README.md                      ← সেটআপ / রান গাইড
├── ModularMonolith.sln            ← Solution
├── Directory.Build.props          ← Shared MSBuild (TargetFramework ইত্যাদি)
├── Dockerfile
├── docker-compose.yml             ← Dev: Postgres + Redis + API
├── docker-compose.prod.yml
├── .dockerignore
├── .env.example                   ← Env টেমপ্লেট (সিক্রেট ছাড়া)
├── .gitignore
│
├── deploy/
│   └── nginx/
│       └── modular-monolith.conf  ← Production Nginx reverse proxy
│
├── docs/
│   └── FOLDER-STRUCTURE-BANGLA.md ← আর্কিটেকচার ব্যাখ্যা (Bangla)
│
├── scripts/
│   └── add-migration.ps1          ← Module-wise EF migration
│
├── src/
│   ├── Api/                       ← Composition root (অ্যাপ এখান থেকে স্টার্ট)
│   │   ├── Program.cs
│   │   ├── Api.csproj
│   │   ├── Api.http
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── appsettings.Production.json
│   │   ├── Extensions/
│   │   │   └── MigrationExtensions.cs
│   │   ├── Middleware/
│   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   └── ExceptionHandlingMiddleware.cs
│   │   └── Properties/
│   │       └── launchSettings.json
│   │
│   ├── BuildingBlocks/            ← সব মডিউলের শেয়ারড কোড
│   │   ├── Domain/
│   │   │   ├── BuildingBlocks.Domain.csproj
│   │   │   └── Common/
│   │   │       ├── AggregateRoot.cs
│   │   │       ├── AuditableEntity.cs
│   │   │       ├── BaseEntity.cs
│   │   │       ├── DomainEvent.cs
│   │   │       ├── IDomainEvent.cs
│   │   │       └── ISoftDelete.cs
│   │   │
│   │   ├── Application/
│   │   │   ├── BuildingBlocks.Application.csproj
│   │   │   ├── Abstractions/
│   │   │   │   ├── IAuditLogger.cs
│   │   │   │   ├── ICacheService.cs
│   │   │   │   ├── ICurrentUserService.cs
│   │   │   │   ├── IDateTimeProvider.cs
│   │   │   │   ├── IEmailSender.cs
│   │   │   │   ├── IModule.cs
│   │   │   │   └── IUserProvisioningService.cs
│   │   │   ├── Authorization/
│   │   │   │   └── PermissionRequirement.cs
│   │   │   └── Common/
│   │   │       ├── Exceptions/
│   │   │       │   └── AppException.cs
│   │   │       └── Models/
│   │   │           ├── ApiResponse.cs
│   │   │           ├── Error.cs
│   │   │           ├── PagedResult.cs
│   │   │           ├── PaginationRequest.cs
│   │   │           └── Result.cs
│   │   │
│   │   ├── Infrastructure/
│   │   │   ├── BuildingBlocks.Infrastructure.csproj
│   │   │   ├── DependencyInjection.cs
│   │   │   ├── Audit/
│   │   │   │   └── NullAuditLogger.cs
│   │   │   ├── Auth/
│   │   │   │   └── CurrentUserService.cs
│   │   │   ├── Authorization/
│   │   │   │   ├── PermissionAuthorizationHandler.cs
│   │   │   │   └── PermissionPolicyProvider.cs
│   │   │   ├── Caching/
│   │   │   │   ├── CacheOptions.cs
│   │   │   │   └── DistributedCacheService.cs
│   │   │   ├── Email/
│   │   │   │   └── LoggingEmailSender.cs
│   │   │   ├── Persistence/
│   │   │   │   └── AuditableInterceptor.cs
│   │   │   └── Time/
│   │   │       └── DateTimeProvider.cs
│   │   │
│   │   └── Shared/
│   │       ├── BuildingBlocks.Shared.csproj
│   │       └── Constants/
│   │           ├── ClaimTypes.cs
│   │           ├── Permissions.cs
│   │           └── Roles.cs
│   │
│   └── Modules/
│       │
│       ├── Identity/              ← Auth: register, login, JWT, refresh, password
│       │   ├── Domain/
│       │   │   ├── Identity.Domain.csproj
│       │   │   ├── Entities/
│       │   │   │   ├── User.cs
│       │   │   │   └── RefreshToken.cs
│       │   │   └── Events/
│       │   │       └── UserRegisteredDomainEvent.cs
│       │   ├── Application/
│       │   │   ├── Identity.Application.csproj
│       │   │   ├── DependencyInjection.cs
│       │   │   ├── Abstractions/
│       │   │   │   ├── IIdentityDbContext.cs
│       │   │   │   ├── IJwtTokenService.cs
│       │   │   │   ├── IPasswordHasher.cs
│       │   │   │   └── IRolePermissionLookup.cs
│       │   │   ├── DTOs/
│       │   │   │   └── AuthDtos.cs
│       │   │   ├── Options/
│       │   │   │   └── JwtOptions.cs
│       │   │   ├── Services/
│       │   │   │   └── AuthService.cs
│       │   │   └── Validators/
│       │   │       └── AuthValidators.cs
│       │   ├── Infrastructure/
│       │   │   ├── Identity.Infrastructure.csproj
│       │   │   ├── DependencyInjection.cs
│       │   │   ├── Persistence/
│       │   │   │   ├── IdentityDbContext.cs          ← schema: identity
│       │   │   │   ├── Configurations/
│       │   │   │   │   ├── UserConfiguration.cs
│       │   │   │   │   └── RefreshTokenConfiguration.cs
│       │   │   │   └── Migrations/
│       │   │   └── Security/
│       │   │       ├── JwtTokenService.cs
│       │   │       ├── PasswordHasher.cs
│       │   │       └── NullRolePermissionLookup.cs
│       │   └── Presentation/
│       │       ├── Identity.Presentation.csproj
│       │       └── Controllers/
│       │           └── AuthController.cs             ← /api/v1/auth/*
│       │
│       ├── Users/                 ← User profile CRUD
│       │   ├── Domain/
│       │   │   ├── Users.Domain.csproj
│       │   │   └── Entities/
│       │   │       └── UserProfile.cs
│       │   ├── Application/
│       │   │   ├── Users.Application.csproj
│       │   │   ├── DependencyInjection.cs
│       │   │   ├── Abstractions/
│       │   │   │   └── IUsersDbContext.cs
│       │   │   ├── DTOs/
│       │   │   │   └── UserDtos.cs
│       │   │   ├── Services/
│       │   │   │   └── UserService.cs
│       │   │   └── Validators/
│       │   │       └── UpdateProfileRequestValidator.cs
│       │   ├── Infrastructure/
│       │   │   ├── Users.Infrastructure.csproj
│       │   │   ├── DependencyInjection.cs
│       │   │   ├── Persistence/
│       │   │   │   ├── UsersDbContext.cs             ← schema: users
│       │   │   │   ├── Configurations/
│       │   │   │   │   └── UserProfileConfiguration.cs
│       │   │   │   └── Migrations/
│       │   │   └── Services/
│       │   │       └── UserProvisioningService.cs
│       │   └── Presentation/
│       │       ├── Users.Presentation.csproj
│       │       └── Controllers/
│       │           └── UsersController.cs            ← /api/v1/users/*
│       │
│       ├── Roles/                 ← Roles, permissions, assign
│       │   ├── Domain/
│       │   │   ├── Roles.Domain.csproj
│       │   │   └── Entities/
│       │   │       ├── Role.cs
│       │   │       ├── Permission.cs
│       │   │       ├── RolePermission.cs
│       │   │       └── UserRole.cs
│       │   ├── Application/
│       │   │   ├── Roles.Application.csproj
│       │   │   ├── DependencyInjection.cs
│       │   │   ├── Abstractions/
│       │   │   │   └── IRolesDbContext.cs
│       │   │   ├── DTOs/
│       │   │   │   └── RoleDtos.cs
│       │   │   └── Services/
│       │   │       └── RoleService.cs
│       │   ├── Infrastructure/
│       │   │   ├── Roles.Infrastructure.csproj
│       │   │   ├── DependencyInjection.cs
│       │   │   ├── Persistence/
│       │   │   │   ├── RolesDbContext.cs             ← schema: roles
│       │   │   │   ├── RolesSeed.cs
│       │   │   │   ├── Configurations/
│       │   │   │   │   └── RoleConfigurations.cs
│       │   │   │   └── Migrations/
│       │   │   └── Services/
│       │   │       └── RolePermissionLookup.cs
│       │   └── Presentation/
│       │       ├── Roles.Presentation.csproj
│       │       └── Controllers/
│       │           └── RolesController.cs            ← /api/v1/roles/*
│       │
│       ├── Notifications/         ← In-app + email notifications
│       │   ├── Domain/
│       │   │   ├── Notifications.Domain.csproj
│       │   │   └── Entities/
│       │   │       └── Notification.cs
│       │   ├── Application/
│       │   │   ├── Notifications.Application.csproj
│       │   │   ├── DependencyInjection.cs
│       │   │   ├── Abstractions/
│       │   │   │   └── INotificationsDbContext.cs
│       │   │   ├── DTOs/
│       │   │   │   └── NotificationDtos.cs
│       │   │   └── Services/
│       │   │       └── NotificationService.cs
│       │   ├── Infrastructure/
│       │   │   ├── Notifications.Infrastructure.csproj
│       │   │   ├── DependencyInjection.cs
│       │   │   └── Persistence/
│       │   │       ├── NotificationsDbContext.cs     ← schema: notifications
│       │   │       └── Migrations/
│       │   └── Presentation/
│       │       ├── Notifications.Presentation.csproj
│       │       └── Controllers/
│       │           └── NotificationsController.cs    ← /api/v1/notifications/*
│       │
│       └── AuditLogs/             ← Action / IP / timestamp audit
│           ├── Domain/
│           │   ├── AuditLogs.Domain.csproj
│           │   └── Entities/
│           │       └── AuditLog.cs
│           ├── Application/
│           │   ├── AuditLogs.Application.csproj
│           │   ├── DependencyInjection.cs
│           │   ├── Abstractions/
│           │   │   └── IAuditLogsDbContext.cs
│           │   ├── DTOs/
│           │   │   └── AuditLogDtos.cs
│           │   └── Services/
│           │       └── AuditLogService.cs
│           ├── Infrastructure/
│           │   ├── AuditLogs.Infrastructure.csproj
│           │   ├── DependencyInjection.cs
│           │   ├── Persistence/
│           │   │   ├── AuditLogsDbContext.cs         ← schema: audit
│           │   │   └── Migrations/
│           │   └── Services/
│           │       └── AuditLogger.cs
│           └── Presentation/
│               ├── AuditLogs.Presentation.csproj
│               └── Controllers/
│                   └── AuditLogsController.cs        ← /api/v1/audit-logs/*
│
└── tests/
    ├── UnitTests/
    │   ├── UnitTests.csproj
    │   ├── Application/
    │   │   └── ResultTests.cs
    │   ├── Domain/
    │   │   ├── UserTests.cs
    │   │   └── RoleTests.cs
    │   └── Infrastructure/
    │       └── PasswordHasherTests.cs
    └── IntegrationTests/
        ├── IntegrationTests.csproj
        ├── CustomWebApplicationFactory.cs
        └── AuthEndpointTests.cs
```

---

## Module → Database schema

| Module | DbContext | Schema |
|--------|-----------|--------|
| Identity | `IdentityDbContext` | `identity` |
| Users | `UsersDbContext` | `users` |
| Roles | `RolesDbContext` | `roles` |
| Notifications | `NotificationsDbContext` | `notifications` |
| AuditLogs | `AuditLogsDbContext` | `audit` |

একই PostgreSQL DB; প্রতি মডিউল আলাদা schema + আলাদা migration।

---

## Dependency নিয়ম

```
Presentation  →  Application  →  Domain
Infrastructure → Application  →  Domain
Api           →  সব Module-এর Infrastructure + Presentation
```

- Module A-এর Infrastructure সরাসরি Module B-এর Infrastructure reference করবে না।
- Cross-module কানেকশন = BuildingBlocks abstraction / Application contract।

---

## Request flow (উদাহরণ: Login)

```
Client
  → Api Middleware (CorrelationId, Exception, Auth, RateLimit)
  → Identity.Presentation / AuthController
  → Identity.Application / AuthService
  → Identity.Infrastructure (DbContext, JWT, PasswordHasher)
  → JSON ApiResponse
```

---

## কোন জিনিস কোথায় রাখবে

| কী বানাচ্ছো | কোথায় |
|-------------|--------|
| Entity / domain rule | `Modules/*/Domain` |
| Service / use-case | `Modules/*/Application/Services` |
| DTO | `Modules/*/Application/DTOs` |
| FluentValidation | `Modules/*/Application/Validators` |
| DbContext / EF config | `Modules/*/Infrastructure/Persistence` |
| Controller | `Modules/*/Presentation/Controllers` |
| Shared Result, cache, email interface | `BuildingBlocks` |
| Program, middleware, Swagger | `src/Api` |
| Unit test | `tests/UnitTests` |
| API / integration test | `tests/IntegrationTests` |

---

## নতুন Module যোগ করার ছোট চেকলিস্ট

1. `src/Modules/YourModule/{Domain,Application,Infrastructure,Presentation}` তৈরি করো  
2. Project reference ঠিক করো (উপরের dependency নিয়ম)  
3. `DependencyInjection.cs` + `AddYourModule()`  
4. `Program.cs`-এ register + `AddApplicationPart`  
5. লাগলে `BuildingBlocks/Shared/Constants/Permissions.cs`-এ permission যোগ করো  

বিস্তারিত ধাপ: [`docs/FOLDER-STRUCTURE-BANGLA.md`](docs/FOLDER-STRUCTURE-BANGLA.md) সেকশন ৬।
