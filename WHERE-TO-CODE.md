# কোথায় কোড লিখবে + কীভাবে লিখবে

এই গাইড তোমাকে বলবে: **কোন ফোল্ডার/ফাইলে কী লিখতে হবে** এবং **কোন প্যাটার্নে লিখবে**।  
পুরো ফাইল ট্রি দেখতে: [`CODE-STRUCTURE.md`](CODE-STRUCTURE.md)

---

## ১) আগে একটা সিদ্ধান্ত নাও

| প্রশ্ন | উত্তর হলে যা করবে |
|--------|-------------------|
| এটা কি নতুন ব্যবসার এরিয়া? (যেমন Orders, Products) | **নতুন Module** বানাও → সেকশন ৫ |
| আগের মডিউলে নতুন API/ফিচার? (যেমন Users-এ নতুন endpoint) | **সেই Module-এর ৪ লেয়ারে** ফাইল যোগ করো → সেকশন ৪ |
| ২+ মডিউল একই জিনিস ব্যবহার করবে? | **BuildingBlocks**-এ রাখো → সেকশন ৬ |
| শুধু middleware / Swagger / startup? | **`src/Api`**-এ রাখো |

**মনে রাখো:** Controller কখনো `src/Api`-তে রাখবে না। Controller সবসময় `Modules/*/Presentation/Controllers`-এ।

---

## ২) ৪ লেয়ার — কী কোথায় যায়

প্রতিটি module-এর ভিতর:

```
Modules/YourModule/
├── Domain/           ← ব্যবসার object ও নিয়ম (DB/HTTP জানে না)
├── Application/      ← use-case, DTO, validation, interface
├── Infrastructure/   ← EF Core, JWT, Redis, বাইরের সার্ভিস
└── Presentation/     ← শুধু HTTP Controller (পাতলা রাখো)
```

### দ্রুত ম্যাপ

| তুমি কী চাও | কোন ফোল্ডার | উদাহরণ ফাইল |
|-------------|-------------|-------------|
| নতুন টেবিল / entity | `Domain/Entities/` | `Order.cs` |
| ব্যবসার method (Create/Update) | একই Entity-তে | `Order.MarkPaid()` |
| Request/Response shape | `Application/DTOs/` | `OrderDtos.cs` |
| ব্যবসার ফ্লো / API লজিক | `Application/Services/` | `OrderService.cs` |
| ইনপুট চেক | `Application/Validators/` | `CreateOrderValidator.cs` |
| DbContext interface | `Application/Abstractions/` | `IOrdersDbContext.cs` |
| আসল DbContext / migration | `Infrastructure/Persistence/` | `OrdersDbContext.cs` |
| EF column config | `Infrastructure/Persistence/Configurations/` | `OrderConfiguration.cs` |
| JWT / email / বাইরের API | `Infrastructure/...` | `SmtpEmailSender.cs` |
| HTTP endpoint | `Presentation/Controllers/` | `OrdersController.cs` |
| DI register | `Application` + `Infrastructure` এর `DependencyInjection.cs` | `AddOrdersModule()` |

### Dependency দিক (ভাঙবে না)

```
Presentation  →  Application  →  Domain
Infrastructure → Application  →  Domain
```

❌ Application থেকে Infrastructure reference করবে না  
❌ এক মডিউলের Infrastructure অন্য মডিউলের Infrastructure ধরবে না  
✅ কথা বলবে interface দিয়ে (`BuildingBlocks` বা Application Abstractions)

---

## ৩) কোড কীভাবে লিখবে (প্রজেক্টের স্টাইল)

এই টেমপ্লেটের স্টাইল অনুসরণ করো — নতুন প্যাটার্ন আবিষ্কার করার দরকার নেই।

### A) Domain Entity

- Property `private set`
- Parameterless private constructor (EF-এর জন্য)
- তৈরি: `static Create(...)`
- পরিবর্তন: method দিয়ে (`Update`, `Activate`)

```csharp
// Modules/Users/Domain/Entities/UserProfile.cs  (প্যাটার্ন)
public sealed class UserProfile : AuditableEntity, ISoftDelete
{
    private UserProfile() { }

    public string FirstName { get; private set; } = string.Empty;

    public static UserProfile Create(...) => new() { ... };

    public void Update(...) { FirstName = firstName.Trim(); }
}
```

### B) Application Service

- Constructor দিয়ে dependency নাও
- `Result` / `Result<T>` ফেরাও (`BuildingBlocks.Application.Common.Models`)
- DB ধরো interface দিয়ে (`IUsersDbContext`)
- Validation লাগলে `IValidator<T>` ব্যবহার করো

```csharp
public sealed class UserService(IUsersDbContext db, ICurrentUserService currentUser, ...)
{
    public async Task<Result<UserProfileDto>> GetMeAsync(CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Result.Failure<UserProfileDto>(Error.Unauthorized("Not authenticated."));
        // ...
        return Result.Success(dto);
    }
}
```

Service DI-তে register:

```csharp
// Modules/Users/Application/DependencyInjection.cs
services.AddScoped<UserService>();
services.AddValidatorsFromAssemblyContaining<UserService>();
```

### C) Presentation Controller

- পাতলা রাখো: শুধু service কল + HTTP status
- Route: `api/v1/...`
- Permission: `[HasPermission(Permissions.Xxx)]`
- Response: `ApiResponse` / `ApiResponse<T>`

```csharp
[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(UserService userService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var result = await userService.GetMeAsync(ct);
        return result.IsSuccess
            ? Ok(ApiResponse<UserProfileDto>.Ok(result.Value))
            : Unauthorized(ApiResponse.Fail(result.Error.Message));
    }
}
```

### D) Infrastructure DbContext

- Schema আলাদা (`users`, `identity`, ...)
- Application-এর interface implement করো

```csharp
public sealed class UsersDbContext(...) : DbContext, IUsersDbContext
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("users");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
    }
}
```

Module register:

```csharp
// Modules/Users/Infrastructure/DependencyInjection.cs
public static IServiceCollection AddUsersModule(...)
{
    services.AddUsersApplication();
    services.AddDbContext<UsersDbContext>(...);
    services.AddScoped<IUsersDbContext>(sp => sp.GetRequiredService<UsersDbContext>());
    return services;
}
```

`Program.cs`-এ:

```csharp
builder.Services.AddUsersModule(builder.Configuration);
// + AddApplicationPart(typeof(UsersController).Assembly)
```

---

## ৪) বিদ্যমান Module-এ নতুন ফিচার (ধাপে ধাপে)

উদাহরণ: **Users**-এ নতুন endpoint — “আমার avatar আপডেট”

### Step 1 — Domain (লাগলে)

`Modules/Users/Domain/Entities/UserProfile.cs`-এ method যোগ করো:

```csharp
public void SetAvatar(string? avatarUrl) => AvatarUrl = avatarUrl?.Trim();
```

শুধু data field নতুন হলে Entity + পরে migration।

### Step 2 — DTO

`Modules/Users/Application/DTOs/UserDtos.cs`-এ request/response যোগ করো।

### Step 3 — Validator (লাগলে)

`Modules/Users/Application/Validators/`-এ FluentValidation class।

### Step 4 — Service

`Modules/Users/Application/Services/UserService.cs`-এ method:

```csharp
public async Task<Result<UserProfileDto>> UpdateAvatarAsync(...)
{
    // validate → load entity → entity.SetAvatar → SaveChanges → Result.Success
}
```

### Step 5 — Controller

`Modules/Users/Presentation/Controllers/UsersController.cs`-এ endpoint।  
Permission লাগলে `[HasPermission(...)]`।

### Step 6 — Permission (নতুন হলে)

1. `src/BuildingBlocks/Shared/Constants/Permissions.cs`-এ constant যোগ  
2. `Roles` seed-এ Admin-কে দাও (`RolesSeed`)  
3. Controller-এ attribute লাগাও

### Step 7 — DB পরিবর্তন হলে migration

```powershell
./scripts/add-migration.ps1 -Module Users -Name AddAvatarField
```

### Step 8 — Test (ভালো অভ্যাস)

- Domain/pure logic → `tests/UnitTests`
- API flow → `tests/IntegrationTests`

---

## ৫) সম্পূর্ণ নতুন Module কীভাবে সাজাবে

উদাহরণ মডিউল নাম: **Orders**

### Folder তৈরি

```
src/Modules/Orders/
├── Domain/
│   └── Entities/Order.cs
├── Application/
│   ├── Abstractions/IOrdersDbContext.cs
│   ├── DTOs/OrderDtos.cs
│   ├── Services/OrderService.cs
│   ├── Validators/...
│   └── DependencyInjection.cs      → AddOrdersApplication()
├── Infrastructure/
│   ├── Persistence/OrdersDbContext.cs   (schema: orders)
│   ├── Persistence/Configurations/
│   ├── Persistence/Migrations/
│   └── DependencyInjection.cs      → AddOrdersModule()
└── Presentation/
    └── Controllers/OrdersController.cs
```

### Project reference

| Project | Reference করে |
|---------|----------------|
| `Orders.Domain` | `BuildingBlocks.Domain` |
| `Orders.Application` | `Orders.Domain` + `BuildingBlocks.Application` |
| `Orders.Infrastructure` | `Orders.Application` + `BuildingBlocks.Infrastructure` |
| `Orders.Presentation` | `Orders.Application` (+ Shared যদি permission লাগে) |
| `Api` | `Orders.Infrastructure` + `Orders.Presentation` |

### Api-তে জোড়া

`src/Api/Program.cs`:

```csharp
builder.Services.AddOrdersModule(builder.Configuration);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(Orders.Presentation.Controllers.OrdersController).Assembly);
```

Migration apply list-এও Orders DbContext যোগ করতে হতে পারে (`Api/Extensions/MigrationExtensions.cs`)।

---

## ৬) BuildingBlocks-এ কখন কোড লিখবে

| রাখবে | রাখবে না |
|--------|----------|
| `Result`, `ApiResponse`, pagination | শুধু Identity-র JWT detail |
| `ICacheService`, `IEmailSender`, `IAuditLogger` | শুধু Users-এর `UserProfile` |
| `Permissions` / `Roles` constants | এক মডিউলের private helper |
| Base entity (`AuditableEntity`) | Module-specific DbContext |

নিয়ম: **২টা বা বেশি মডিউল** একই জিনিস চাইলে BuildingBlocks।

---

## ৭) Api ফোল্ডারে কী লিখবে / কী লিখবে না

| লিখবে (`src/Api`) | লিখবে না |
|-------------------|----------|
| `Program.cs` DI + middleware pipeline | Business logic |
| `Middleware/` | Entity / Service |
| `Extensions/` (migration helper) | Controller |
| `appsettings*.json` | Module-specific EF config |

---

## ৮) “আমি কী চাই → কোথায় যাব” চিটশিট

| চাহিদা | ফোল্ডার / ফাইল |
|--------|----------------|
| নতুন REST endpoint | `Presentation/Controllers/*Controller.cs` |
| Endpoint-এর আসল কাজ | `Application/Services/*Service.cs` |
| JSON body shape | `Application/DTOs/` |
| Body validation | `Application/Validators/` |
| নতুন টেবিল | `Domain/Entities/` + `Infrastructure/Persistence/` |
| কলাম rename/add | Entity + Configuration + migration script |
| Login / token | `Modules/Identity/...` |
| Role/permission | `Modules/Roles/...` + `BuildingBlocks/Shared/Constants/Permissions.cs` |
| ইমেইল পাঠানো abstraction | `BuildingBlocks` interface + Infrastructure implementation |
| Global error format | `Api/Middleware/ExceptionHandlingMiddleware.cs` |
| Config / connection string | `Api/appsettings*.json` বা `.env` |
| Docker চালানো | root `docker-compose.yml` |

---

## ৯) ছোট উদাহরণ ফ্লো (মনে রাখার জন্য)

```
1) Clients POST/GET
2) Presentation/Controller     ← route, auth, ApiResponse
3) Application/Service         ← Result, validation, orchestration
4) Domain/Entity               ← ব্যবসার নিয়ম
5) Infrastructure/DbContext    ← PostgreSQL save/load
6) Response ফেরত
```

এক লাইনে:

> **Controller পাতলা · Service-এ ফ্লো · Entity-তে নিয়ম · Infrastructure-এ DB · শেয়ারড জিনিস BuildingBlocks-এ।**

---

## ১০) সম্পর্কিত ডক

| ফাইল | কী আছে |
|------|--------|
| [`CODE-STRUCTURE.md`](CODE-STRUCTURE.md) | পুরো ফাইল ট্রি |
| [`docs/FOLDER-STRUCTURE-BANGLA.md`](docs/FOLDER-STRUCTURE-BANGLA.md) | আর্কিটেকচার গভীর ব্যাখ্যা |
| [`README.md`](README.md) | রান / Docker / migration কমান্ড |

সন্দেহ হলে আগে **Users** মডিউল কপি করে দেখো — এটাই সবচেয়ে পরিষ্কার রেফারেন্স ইমপ্লিমেন্টেশন।
