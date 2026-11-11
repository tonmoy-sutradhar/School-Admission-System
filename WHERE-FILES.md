# WHERE-FILES — কোন জায়গায় কোন ফাইল?

দ্রুত খুঁজে পাওয়ার ম্যাপ: **server start, login/identity, route, page, API, config** কোথায় আছে।

> কোড **কীভাবে** লিখবে → [`WHERE-TO-CODE.md`](WHERE-TO-CODE.md)  
> পুরো ফাইল ট্রি → [`CODE-STRUCTURE.md`](CODE-STRUCTURE.md)  
> কীভাবে রান করবে → [`HOW-TO-RUN.md`](HOW-TO-RUN.md)

---

## ০) এক নজরে — দুই রকম “identity”

| ধরন | কী | Frontend use করে? | কোথায় |
|------|----|-------------------|--------|
| **Assignment UI** | ১৭-digit birth-reg → `X-Student-Id` header | ✅ হ্যাঁ | নিচের “Student ID” সেকশন |
| **Template JWT login** | email/password → JWT | ❌ না (শুধু backend টেমপ্লেট) | `Modules/Identity` |

**মনে রাখো:** School Admission UI-তে আলাদা login page নেই। উপরের বারে student ID দিলেই চলে।

---

## ১) Server কীভাবে start হয়

| কাজ | ফাইল |
|-----|------|
| API entry / DI / middleware / health | `backend/src/Api/Program.cs` |
| API project | `backend/src/Api/Api.csproj` |
| Local port / launch | `backend/src/Api/Properties/launchSettings.json` |
| Solution | `backend/ModularMonolith.sln` |
| Backend Docker image | `backend/Dockerfile` |
| সব একসাথে (Postgres + API + UI) | root `docker-compose.yml` |
| Prod override | `docker-compose.prod.yml` |

**চালানো:**

```bash
docker compose up --build
# বা
dotnet run --project backend/src/Api/Api.csproj
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| Backend | http://localhost:8080 |
| Health | http://localhost:8080/health |
| Swagger | http://localhost:8080/swagger |

---

## ২) Login / Student ID — কোন ফাইলে কী আছে

### A) যা UI আসলে ব্যবহার করে (Student ID)

| কাজ | ফাইল |
|-----|------|
| Student ID state + `localStorage` | `frontend/src/context/StudentContext.tsx` |
| উপরের বার (১৭ digit input) | `frontend/src/components/StudentIdBar.tsx` |
| সব API কলে `X-Student-Id` লাগানো | `frontend/src/api/client.ts` |
| Header পড়ে request-এ রাখা | `backend/src/Api/Middleware/StudentIdMiddleware.cs` |
| Request-scoped student holder | `backend/src/Api/StudentContext.cs` |
| Shared interface | `backend/src/BuildingBlocks/Application/Abstractions/IStudentContext.cs` |

### B) JWT login (টেমপ্লেট — admission UI use করে না)

| কাজ | ফাইল |
|-----|------|
| HTTP endpoints (`/api/v1/auth/*`) | `backend/src/Modules/Identity/Presentation/Controllers/AuthController.cs` |
| Login / register / refresh লজিক | `.../Identity/Application/Services/AuthService.cs` |
| Request DTO | `.../Identity/Application/DTOs/AuthDtos.cs` |
| Validation | `.../Identity/Application/Validators/AuthValidators.cs` |
| JWT issue | `.../Identity/Infrastructure/Security/JwtTokenService.cs` |
| Password hash | `.../Identity/Infrastructure/Security/PasswordHasher.cs` |
| User / RefreshToken entity | `.../Identity/Domain/Entities/` |
| JWT DI register | `.../Identity/Infrastructure/DependencyInjection.cs` |

**Auth API paths:**

| Method | Path |
|--------|------|
| POST | `/api/v1/auth/register` |
| POST | `/api/v1/auth/login` |
| POST | `/api/v1/auth/refresh` |
| POST | `/api/v1/auth/logout` |
| POST | `/api/v1/auth/forgot-password` |
| POST | `/api/v1/auth/reset-password` |
| POST | `/api/v1/auth/verify-email` |
| POST | `/api/v1/auth/change-password` |
| GET | `/api/v1/auth/google` |

---

## ৩) Routes কোথায়

### A) Frontend (React) routes

ফাইল: **`frontend/src/App.tsx`**  
Router bootstrap: `frontend/src/main.tsx`

| URL | Page ফাইল |
|-----|-----------|
| `/` | `frontend/src/pages/ClassListPage.tsx` |
| `/admin/classes` | `frontend/src/pages/CreateClassPage.tsx` |
| `/holds/:holdId` | `frontend/src/pages/HoldDetailPage.tsx` |
| `/me/holds` | `frontend/src/pages/MyHoldsPage.tsx` |

নতুন page চাইলে: `pages/`-এ কম্পোনেন্ট + `App.tsx`-এ `<Route>` যোগ করো।

### B) Admission API routes (assignment — এটাই মূল প্রোডাক্ট)

| Method | Path | Controller ফাইল |
|--------|------|-----------------|
| POST | `/classes` | `backend/src/Modules/Admissions/Presentation/Controllers/ClassesController.cs` |
| GET | `/classes` | একই |
| GET | `/classes/{classId}` | একই |
| POST | `/classes/{classId}/holds` | একই (`X-Student-Id` লাগে) |
| GET | `/holds/{holdId}` | `.../HoldsController.cs` |
| POST | `/holds/{holdId}/confirm` | একই (`X-Student-Id`) |
| DELETE | `/holds/{holdId}` | একই (`X-Student-Id`) |
| GET | `/me/holds` | `.../MeController.cs` (`X-Student-Id`) |
| GET | `/health` | `backend/src/Api/Program.cs` |

**বিজনেস লজিক (service):**

| কাজ | ফাইল |
|-----|------|
| Class create / list | `.../Admissions/Application/Services/ClassService.cs` |
| Hold / confirm / cancel | `.../Admissions/Application/Services/HoldService.cs` |
| Entity | `.../Admissions/Domain/Entities/` (`AdmissionClass`, `SeatHold`) |
| DB | `.../Admissions/Infrastructure/Persistence/AdmissionsDbContext.cs` |
| Hold expire worker | `.../Admissions/Infrastructure/Background/HoldExpirationWorker.cs` |

### C) অন্য template API (`/api/v1/...`)

| Prefix | Controller |
|--------|------------|
| `/api/v1/auth/*` | Identity → `AuthController.cs` |
| `/api/v1/users/*` | Users → `UsersController.cs` |
| `/api/v1/roles/*` | Roles → `RolesController.cs` |
| `/api/v1/notifications/*` | Notifications → `NotificationsController.cs` |
| `/api/v1/audit-logs` | AuditLogs → `AuditLogsController.cs` |

---

## ৪) Frontend ফাইল ম্যাপ

```
frontend/src/
├── main.tsx                 ← React root + BrowserRouter
├── App.tsx                  ← Nav + Routes + StudentProvider
├── index.css                ← Global styles
├── api/client.ts            ← fetch + X-Student-Id
├── context/StudentContext.tsx
├── types/admission.ts       ← TS types
├── components/
│   ├── StudentIdBar.tsx
│   ├── Countdown.tsx
│   ├── SeatSummary.tsx
│   └── ErrorBanner.tsx
└── pages/
    ├── ClassListPage.tsx
    ├── CreateClassPage.tsx
    ├── HoldDetailPage.tsx
    └── MyHoldsPage.tsx
```

অন্যান্য: `frontend/vite.config.ts`, `frontend/.env.example` (`VITE_API_BASE_URL`), `frontend/Dockerfile`, `frontend/nginx.conf`

---

## ৫) Backend ফোল্ডার নিয়ম (মডিউল)

প্রতিটি module:

```
backend/src/Modules/<Name>/
├── Domain/           ← Entity / business rule
├── Application/      ← Service, DTO, Validator
├── Infrastructure/   ← EF Core, DB, external
└── Presentation/     ← Controller only
```

| কী লিখছো | কোথায় |
|----------|--------|
| নতুন HTTP endpoint | `Presentation/Controllers/` |
| আসল কাজ / flow | `Application/Services/` |
| JSON body shape | `Application/DTOs/` |
| Validation | `Application/Validators/` |
| নতুন টেবিল | `Domain/Entities/` + `Infrastructure/Persistence/` |
| Startup / middleware | `backend/src/Api/` |
| শেয়ারড জিনিস | `backend/src/BuildingBlocks/` |

**Controller কখনো `Api/` ফোল্ডারে রাখবে না** — সবসময় `Modules/*/Presentation/Controllers`।

---

## ৬) Config / Env / Docker

| কী | ফাইল |
|----|------|
| Compose env (DB, JWT, ports, hold duration) | root `.env` / `.env.example` |
| Frontend API base URL | `frontend/.env` / `.env.example` |
| Backend settings | `backend/src/Api/appsettings.json` (+ Development/Production) |
| Dev compose | `docker-compose.yml` |
| Prod compose | `docker-compose.prod.yml` |
| Nginx sample | `deploy/nginx/modular-monolith.conf` |
| EF migration helper | `backend/scripts/add-migration.ps1` |

---

## ৭) “আমি কী চাই → কোন ফাইলে যাব”

| চাই | যাও |
|-----|-----|
| UI-তে student ID বদলানো | `frontend/src/context/StudentContext.tsx` + `StudentIdBar.tsx` |
| API-তে student header পাঠানো | `frontend/src/api/client.ts` |
| Class list page | `frontend/src/pages/ClassListPage.tsx` |
| নতুন React route | `frontend/src/App.tsx` |
| Class create / list API | `ClassesController.cs` + `ClassService.cs` |
| Hold confirm / cancel | `HoldsController.cs` + `HoldService.cs` |
| My holds API | `MeController.cs` |
| JWT login endpoint | `AuthController.cs` (`/api/v1/auth/login`) |
| Server start / middleware | `backend/src/Api/Program.cs` |
| Hold expire background job | `HoldExpirationWorker.cs` |
| DB schema / migration (Admissions) | `Admissions/.../Infrastructure/Persistence/` |
| Docker দিয়ে চালানো | `docker-compose.yml` + `HOW-TO-RUN.md` |
| API example (Postman style) | `docs/POSTMAN-API.md` |
| Assumption (no login ইত্যাদি) | `ASSUMPTIONS.md` |

---

## ৮) সম্পর্কিত ডক

| ফাইল | কী আছে |
|------|--------|
| [`README.md`](README.md) | Quick start + demo + API table |
| [`HOW-TO-RUN.md`](HOW-TO-RUN.md) | Bangla/English রান গাইড |
| [`WHERE-TO-CODE.md`](WHERE-TO-CODE.md) | কোথায় কোড লিখবে + প্যাটার্ন |
| [`CODE-STRUCTURE.md`](CODE-STRUCTURE.md) | পুরো backend ফাইল ট্রি |
| [`ASSUMPTIONS.md`](ASSUMPTIONS.md) | No-login, hold rules, HTTP codes |
| [`docs/POSTMAN-API.md`](docs/POSTMAN-API.md) | Admission API examples |
| [`docs/SCHOOL-ADMISSION-SCHEMA.md`](docs/SCHOOL-ADMISSION-SCHEMA.md) | DB tables |
| [`docs/FOLDER-STRUCTURE-BANGLA.md`](docs/FOLDER-STRUCTURE-BANGLA.md) | Architecture (Bangla) |

---

এক লাইনে:

> **UI identity = StudentContext + X-Student-Id · Admission routes = Admissions Controllers · JWT login = Identity AuthController (টেমপ্লেট) · Server start = Program.cs · React routes = App.tsx**
