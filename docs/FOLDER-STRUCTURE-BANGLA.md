# Modular Monolith Backend — সহজ গাইড

এই ফাইলে তিনটা জিনিস আছে:

1. **Folder structure** কেমন
2. **নতুন feature** এলে folder কীভাবে সাজাবে
3. **Request** এলে সিস্টেম ভিতরে কীভাবে কাজ করে

---

## ১) পুরো প্রজেক্ট এক নজরে

```
Tonmoy Backend Templet C/
├── src/                          ← সব আসল কোড
│   ├── Api/                      ← অ্যাপ শুরু হয় এখান থেকে (Composition Root)
│   ├── BuildingBlocks/           ← সব মডিউলের শেয়ারড জিনিস
│   └── Modules/                  ← ব্যবসার মডিউলগুলো
│       ├── Identity/             ← লগইন, রেজিস্টার, JWT
│       ├── Users/                ← ইউজার প্রোফাইল
│       ├── Roles/                ← রোল ও পারমিশন
│       ├── Notifications/        ← ইমেইল / ইন-অ্যাপ নোটিফিকেশন
│       └── AuditLogs/            ← কে কী করেছে — লগ
├── tests/                        ← টেস্ট
│   ├── UnitTests/
│   └── IntegrationTests/
├── deploy/nginx/                 ← সার্ভারে Nginx কনফিগ
├── docs/                         ← ডকুমেন্টেশন (এই ফাইল)
├── scripts/                      ← migration স্ক্রিপ্ট
├── docker-compose.yml            ← Postgres + Redis + API একসাথে চালানো
├── Dockerfile
├── .env / .env.example           ← সিক্রেট ও এনভায়রনমেন্ট
├── README.md                     ← ইংরেজি সেটআপ গাইড
└── ModularMonolith.sln           ← Visual Studio / Rider সলিউশন
```

**মনে রাখো:**  
`Api` = রান্নাঘরের মেইন সুইচ।  
`BuildingBlocks` = সবার কাজে লাগে এমন টুলবক্স।  
`Modules/*` = আলাদা আলাদা ব্যবসার ঘর।

---

## ২) প্রতিটা Module-এর ভিতরের ৪টা লেয়ার

প্রতিটি মডিউল (যেমন `Identity`, `Users`) একই কাঠামো অনুসরণ করে:

```
Modules/Identity/
├── Domain/           ← ব্যবসার নিয়ম (Entity, Event) — DB/API জানে না
├── Application/      ← Use-case / Service / DTO / Validation
├── Infrastructure/   ← EF Core, JWT, Password hash, DB
└── Presentation/     ← Controller (HTTP endpoint)
```

### সহজ মানে

| লেয়ার | কাজ | উদাহরণ |
|--------|------|--------|
| **Domain** | ব্যবসার আসল অবজেক্ট ও নিয়ম | `User.Register()`, `RefreshToken` revoke |
| **Application** | API কী করতে চায় — সার্ভিস লজিক | `AuthService.LoginAsync()` |
| **Infrastructure** | বাইরের জগতট (DB, Redis, JWT) | `IdentityDbContext`, `JwtTokenService` |
| **Presentation** | ইন্টারনেট থেকে রিকোয়েস্ট নেয় | `AuthController` → `/api/v1/auth/login` |

### ডিপেন্ডেন্সি রুল (খুবই গুরুত্বপূর্ণ)

```
Presentation  →  Application  →  Domain
Infrastructure → Application  →  Domain

❌ Module A-এর Infrastructure → Module B-এর Infrastructure  (নিষেধ)
✅ Module আলাদা থাকবে; শুধু Abstraction / BuildingBlocks দিয়ে কথা বলবে
```

এজন্য নতুন মডিউল যোগ করলে অন্য মডিউল ভাঙে না।

---

## ৩) BuildingBlocks কী?

```
BuildingBlocks/
├── Domain/          → BaseEntity, AuditableEntity, DomainEvent
├── Application/     → Result, ApiResponse, Pagination, ICacheService, IEmailSender...
├── Infrastructure/  → CurrentUser, Redis/Memory cache, Exception helpers, Audit interceptor
└── Shared/          → Roles/Permissions কনস্ট্যান্ট
```

**কখন BuildingBlocks-এ রাখবে?**  
যখন **২টা বা তার বেশি মডিউল** একই জিনিস ব্যবহার করে।

**কখন রাখবে না?**  
শুধু Identity-র জিনিস হলে Identity মডিউলেই রাখো।

---

## ৪) Request এলে কীভাবে কাজ করে? (Flow)

উদাহরণ: ইউজার লগইন করল

```
1) Browser/Postman
      POST /api/v1/auth/login
              │
2) Api (Program.cs)
      Middleware: CorrelationId → ExceptionHandler → Auth → RateLimit
              │
3) Identity.Presentation
      AuthController.Login(...)
              │
4) Identity.Application
      AuthService.LoginAsync(...)
        - FluentValidation
        - User খুঁজে Password verify
        - Role/Permission নেয় (Roles মডিউলের abstraction দিয়ে)
        - JWT + Refresh token তৈরি
              │
5) Identity.Infrastructure
      IdentityDbContext (PostgreSQL, schema: identity)
      PasswordHasher / JwtTokenService
              │
6) Response
   { success, data: { accessToken, refreshToken, ... }, correlationId }
```

### মডিউলগুলো কীভাবে জোড়া লাগে (Register এর সময়)

```
Identity (নতুন User তৈরি)
    └─ IUserProvisioningService  (BuildingBlocks abstraction)
           ├─ Users মডিউল → UserProfile তৈরি
           └─ Roles মডিউল → "User" রোল অ্যাসাইন
```

Infrastructure একে অপরকে সরাসরি ধরে না — **interface** দিয়ে কানেক্ট হয়।

---

## ৫) Database কীভাবে ভাগ করা?

প্রতি মডিউলের নিজের `DbContext` + নিজের **schema**:

| Module | DbContext | Schema |
|--------|-----------|--------|
| Identity | `IdentityDbContext` | `identity` |
| Users | `UsersDbContext` | `users` |
| Roles | `RolesDbContext` | `roles` |
| Notifications | `NotificationsDbContext` | `notifications` |
| AuditLogs | `AuditLogsDbContext` | `audit` |

একই PostgreSQL ডাটাবেজ, কিন্তু টেবিল আলাদা schema-তে।  
Migration-ও **মডিউল আলাদা আলাদা**।

নতুন migration:

```powershell
./scripts/add-migration.ps1 -Module Identity -Name AddSomething
```

---

## ৬) নতুন Module কীভাবে সাজাবে? (চেকলিস্ট)

ধরো নতুন মডিউল লাগবে: **Orders**

### Step A — Folder তৈরি

```
src/Modules/Orders/
  Domain/
  Application/
  Infrastructure/
  Presentation/
```

### Step B — প্রজেক্ট রেফারেন্স

```
Orders.Domain          → BuildingBlocks.Domain
Orders.Application     → Orders.Domain + BuildingBlocks.Application
Orders.Infrastructure  → Orders.Application + BuildingBlocks.Infrastructure
Orders.Presentation    → Orders.Application
Api                    → Orders.Infrastructure + Orders.Presentation
```

### Step C — কোড রাখার জায়গা

```
Domain/Entities/Order.cs
Application/Services/OrderService.cs
Application/DTOs/...
Application/Abstractions/IOrdersDbContext.cs
Infrastructure/Persistence/OrdersDbContext.cs
Infrastructure/DependencyInjection.cs   ← AddOrdersModule()
Presentation/Controllers/OrdersController.cs
```

### Step D — Api-তে রেজিস্টার

`Program.cs`-এ:

```csharp
builder.Services.AddOrdersModule(builder.Configuration);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(OrdersController).Assembly);
```

### Step E — Permission (লাগলে)

`BuildingBlocks/Shared/Constants/Permissions.cs`-এ নতুন permission যোগ করো,  
Roles seed-এ Admin-কে দাও, Controller-এ `[HasPermission("orders.view")]` লাগাও।

---

## ৭) কোন ফাইল কোথায় রাখবে? (দ্রুত সিদ্ধান্ত)

| তুমি কী বানাচ্ছো? | কোথায় রাখবে? |
|-------------------|--------------|
| Entity / ব্যবসার নিয়ম | `Module/Domain` |
| Service / Use-case | `Module/Application/Services` |
| Request/Response DTO | `Module/Application/DTOs` |
| FluentValidation | `Module/Application/Validators` |
| EF Configuration / DbContext | `Module/Infrastructure/Persistence` |
| JWT, Email, Redis implementation | সংশ্লিষ্ট `Infrastructure` বা `BuildingBlocks.Infrastructure` |
| Controller / endpoint | `Module/Presentation/Controllers` |
| সবার Result/Pagination | `BuildingBlocks` |
| Program, Swagger, Middleware | `src/Api` |
| Unit test (pure logic) | `tests/UnitTests` |
| API test | `tests/IntegrationTests` |

---

## ৮) Api ফোল্ডারের কাজ

```
src/Api/
├── Program.cs                 ← DI + Middleware + Module register
├── Middleware/                ← Exception, CorrelationId
├── Extensions/                ← Migration apply
├── appsettings*.json          ← কনফিগ
└── Properties/launchSettings.json
```

**মনে রেখো:** Controller এখানে রাখবে না (টেমপ্লেটে মডিউলের Presentation-এ আছে)।  
`Api` শুধু সবকিছু জোড়া লাগায়।

---

## ৯) Config / Secret কোথায়?

| জায়গা | কখন |
|--------|------|
| `.env` | Docker Compose চালানোর সময় |
| `.env.example` | টিমে শেয়ার করার টেমপ্লেট (সিক্রেট ছাড়া) |
| `appsettings.json` | ডিফল্ট কনফিগ |
| `appsettings.Development.json` | লোকাল ডেভ |
| User Secrets / Server env vars | প্রোডাকশন সিক্রেট |

কখনো Git-এ আসল পাসওয়ার্ড/JWT secret কমিট করবে না। `.env` ইতিমধ্যে `.gitignore`-এ আছে।

---

## ১০) চালানোর ছোট রোডম্যাপ

```bash
# 1) env তৈরি
cp .env.example .env

# 2) সব সার্ভিস চালু
docker compose up --build

# 3) ব্রাউজার
http://localhost:8080/swagger
```

লোকাল শুধু API চালাতে চাইলে:

```bash
docker compose up postgres redis -d
dotnet run --project src/Api/Api.csproj
```

---

## ১১) এক লাইনে সারাংশ

> **Presentation** রিকোয়েস্ট নেয় → **Application** ব্যবসা চালায় → **Domain** নিয়ম ধরে → **Infrastructure** DB/Redis/JWT করে।  
> নতুন ফিচার = নতুন Module (৪ লেয়ার) অথবা পুরনো Module-এর সঠিক লেয়ারে ফাইল যোগ।  
> শেয়ারড জিনিস = **BuildingBlocks**।  
> সব জোড়া লাগে **Api/Program.cs**-এ।

এই রুলগুলো মেনে folder সাজালে প্রজেক্ট বড় হলেও পরিষ্কার থাকবে।
