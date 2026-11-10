# School Admission System — এই Template-এ কীভাবে Apply করবে

> Source: `My Doc/Asst_Software_Engineer_Project_Assignment.pdf`  
> Deadline: **11 November 2026, 11:59:59 PM**  
> Stack: **.NET 8+ · PostgreSQL · React 18+ (Vite) · Docker**  
> এই ফাইল শুধু **plan / structure / mapping** — এখনো business code লেখা হয়নি।

---

## ০) এক নজরে Verdict

Assignment চাইছে **School Admission System** (backend + React UI + Docker)।  
তোমার repo এখন **Modular Monolith (.NET 9) template** — এটা ভালো foundation, কিন্তু assignment-এর **repo layout, API path, এবং auth model** আলাদা।

| Assignment চাইছে | Template এখন যা আছে | কী করবে |
|------------------|---------------------|---------|
| Root-এ `backend/` + `frontend/` | `src/` (শুধু backend) | Repo restructure |
| `POST /classes`, `GET /holds/...` (prefix ছাড়া) | `/api/v1/...` | **Contract হুবহু** মানতে হবে — automated test চলবে |
| Login নেই · `X-Student-Id` header | JWT Identity / Roles | নতুন identity middleware; JWT login এই assignment-এ লাগবে না |
| Seat hold + expiry + concurrency | Users/Roles/Notifications modules | নতুন **Admissions** (বা Classes) module |
| `docker compose up` → Postgres + API + Frontend | Postgres + Redis + API | Frontend service যোগ; Redis optional রাখতে পারো |
| `ASSUMPTIONS.md`, `AI_USAGE.md`, ≥3 backend test (1 concurrency) | Unit/Integration test scaffold আছে | Docs + concurrency test বাধ্যতামূলক |

**Strategy:** Template-এর **BuildingBlocks + ৪-layer module pattern** রাখো। Assignment-specific domain নতুন module-এ বানাও। Repo root assignment-এর মতো সাজাও।

---

## ১) Assignment কী চাইছে (Business Summary)

বেসরকারি school — online ভর্তি, **first-come-first-served** (lottery নেই)।

1. Admin UI থেকে class তৈরি করে (প্রতি shift = আলাদা class, যেমন `Class 6 - Morning Shift`, seat = 120)।
2. অভিভাবক জন্ম-নিবন্ধন নম্বর (১৭ অঙ্ক) দিয়ে নিজেকে identify করে — **login নেই**।
3. আবেদন করলে seat **HELD** হয় নির্দিষ্ট সময়ের জন্য (`HOLD_DURATION_SECONDS`, default **600**)।
4. সময়ের মধ্যে "ভর্তি ফি দিন" → **CONFIRMED** (আসল payment gateway লাগবে না)।
5. সময় শেষ / বাতিল → seat আবার খালি। Expiry **নিজে থেকে** হবে — কেউ API call করবে না।

### বাধ্যতামূলক Backend Rules

1. যেকোনো concurrent load-এ `heldSeats + confirmedSeats ≤ totalSeats`।
2. একই DB-তে **দুইটা backend process** আলাদা port-এ চালালেও overbook হবে না (DB-level lock/constraint দিয়ে নিশ্চিত করো → `ASSUMPTIONS.md`-এ লেখো)।
3. এক student এক class-এ একসাথে একটার বেশি **active** (HELD অথবা CONFIRMED) রাখতে পারবে না।
4. শুধু নিজের, মেয়াদ-না-পেরোনো `HELD` confirm করা যাবে।
5. Confirm একই hold-এ বহুবার এলেও `confirmedSeats` একবারই বাড়বে (idempotent)।
6. App restart / `docker compose down` + `up` এ data হারাবে না (Postgres volume)।

### বাধ্যতামূলক UI Rules

1. সব কাজ UI থেকে — Postman/DB ছাড়া demo চলবে।
2. Hold expire হলে reload ছাড়াই `EXPIRED` + Pay button বন্ধ।
3. Class seat count কয়েক সেকেন্ড পরপর refresh (polling যথেষ্ট)।
4. Browser বন্ধ/খুললেও student নিজের ongoing application দেখতে পাবে (student id persist + `/me/holds`)।
5. Error ব্যবহারকারীর ভাষায় — blank page / শুধু console error চলবে না।

---

## ২) API Contract (হুবহু মানতে হবে)

Base URL: `http://localhost:8080`  
সব JSON। Student identity: header **`X-Student-Id`** = ১৭-digit birth registration number।  
Response-এ **extra field OK**; নিচের field **বাদ/rename করা যাবে না**।  
ID format তোমার ইচ্ছা (`c_123`, `h_789` শুধু উদাহরণ)।

| # | Method & Path | কাজ | Notes |
|---|---------------|-----|-------|
| 1 | `POST /classes` | নতুন class | Body: `{ "name", "totalSeats" }` → **201** `{ id, name, totalSeats }` |
| 2 | `GET /classes` | তালিকা + seat হিসাব | **200** array: `id, name, totalSeats, heldSeats, confirmedSeats, availableSeats` |
| 3 | `GET /classes/{classId}` | এক class | একই seat fields |
| 4 | `POST /classes/{classId}/holds` | আবেদন (hold) | Header `X-Student-Id` → **201** `{ holdId, classId, studentId, status:"HELD", expiresAt }` · seat না থাকলে **409** |
| 5 | `GET /holds/{holdId}` | hold status | `status`: `HELD` \| `CONFIRMED` \| `EXPIRED` \| `CANCELLED` |
| 6 | `POST /holds/{holdId}/confirm` | ফি জমা / confirm | Header `X-Student-Id` → **200** `{ holdId, status:"CONFIRMED" }` |
| 7 | `DELETE /holds/{holdId}` | বাতিল | Header `X-Student-Id` → **204** |
| 8 | `GET /me/holds` | আমার সব আবেদন | Header `X-Student-Id` → array with `holdId, classId, className, status, expiresAt` |

### Template থেকে গুরুত্বপূর্ণ ফারাক

- Route হবে **`/classes`**, **`/holds`**, **`/me/holds`** — **`/api/v1/...` নয়** (automated grader/test ভাঙবে)।
- Response wrapper (`ApiResponse { success, data, ... }`) assignment example-এ নেই।  
  **Recommendation:** এই endpoints-এ **raw JSON** ফেরাও যেমন spec-এ আছে। Internal-এ `Result<T>` ব্যবহার করতে পারো; controller-এ unwrap করে contract shape দাও।

---

## ৩) Target Repo Structure (Assignment + Template একসাথে)

Assignment বলে: repo-তে **`backend/`** আর **`frontend/`** থাকবে।  
তাই বর্তমান template কে `backend/`-এর ভিতরে নিয়ে যাবে (বা সমতুল্য map করবে)।

```
SchoolAdmission/                          ← final public GitHub repo root
├── docker-compose.yml                    ← postgres + backend + frontend (শুধু `docker compose up`)
├── .env.example
├── README.md                             ← Docker run steps, frontend URL, test how-to
├── ASSUMPTIONS.md                        ← বাধ্যতামূলক
├── AI_USAGE.md                           ← বাধ্যতামূলক
├── My Doc/                               ← (optional, private রাখো — PDF share নিষেধ)
│   └── Asst_Software_Engineer_Project_Assignment.pdf
│
├── backend/                              ← বর্তমান Modular Monolith এখানে যাবে
│   ├── ModularMonolith.sln               ← নাম চাইলে SchoolAdmission.sln
│   ├── Directory.Build.props             ← net9.0 (assignment .NET 8+ OK)
│   ├── Dockerfile
│   ├── scripts/
│   │   └── add-migration.ps1
│   ├── src/
│   │   ├── Api/                          ← composition root · port 8080
│   │   │   ├── Program.cs
│   │   │   ├── Middleware/
│   │   │   │   ├── CorrelationIdMiddleware.cs      ← আছে, রাখো
│   │   │   │   ├── ExceptionHandlingMiddleware.cs  ← আছে; error message human-readable রাখো
│   │   │   │   └── StudentIdMiddleware.cs          ← নতুন: X-Student-Id parse/validate
│   │   │   ├── Extensions/
│   │   │   └── appsettings*.json
│   │   │
│   │   ├── BuildingBlocks/               ← রাখো (Result, pagination, base entity, time…)
│   │   │   ├── Domain/
│   │   │   ├── Application/
│   │   │   │   └── Abstractions/
│   │   │   │       └── IStudentContext.cs          ← নতুন (current X-Student-Id)
│   │   │   ├── Infrastructure/
│   │   │   └── Shared/
│   │   │
│   │   └── Modules/
│   │       ├── Admissions/               ← ★ এই assignment-এর মূল মডিউল (নতুন)
│   │       │   ├── Domain/
│   │       │   │   ├── Entities/
│   │       │   │   │   ├── AdmissionClass.cs       ← name, totalSeats, …
│   │       │   │   │   └── SeatHold.cs             ← status, expiresAt, studentId, …
│   │       │   │   └── Enums/
│   │       │   │       └── HoldStatus.cs           ← HELD, CONFIRMED, EXPIRED, CANCELLED
│   │       │   ├── Application/
│   │       │   │   ├── Abstractions/
│   │       │   │   │   └── IAdmissionsDbContext.cs
│   │       │   │   ├── DTOs/
│   │       │   │   │   └── AdmissionDtos.cs         ← contract-matching shapes
│   │       │   │   ├── Services/
│   │       │   │   │   ├── ClassService.cs
│   │       │   │   │   └── HoldService.cs           ← hold/confirm/cancel/list + concurrency
│   │       │   │   ├── Validators/
│   │       │   │   └── DependencyInjection.cs
│   │       │   ├── Infrastructure/
│   │       │   │   ├── Persistence/
│   │       │   │   │   ├── AdmissionsDbContext.cs   ← schema: admissions
│   │       │   │   │   ├── Configurations/
│   │       │   │   │   └── Migrations/
│   │       │   │   ├── Background/
│   │       │   │   │   └── HoldExpirationWorker.cs  ← background expire (বা lazy expire)
│   │       │   │   └── DependencyInjection.cs
│   │       │   └── Presentation/
│   │       │       └── Controllers/
│   │       │           ├── ClassesController.cs    ← /classes*
│   │       │           ├── HoldsController.cs      ← /holds*
│   │       │           └── MeController.cs         ← /me/holds
│   │       │
│   │       ├── Identity/                 ← এই assignment-এ JWT login লাগবে না
│   │       ├── Users/                    ← optional: রাখো বা solution থেকে বাদ দাও
│   │       ├── Roles/
│   │       ├── Notifications/
│   │       └── AuditLogs/                ← optional: hold/confirm audit চাইলে ব্যবহার
│   │
│   └── tests/
│       ├── UnitTests/
│       │   └── Domain/…                  ← entity rules
│       └── IntegrationTests/
│           ├── ClassesEndpointTests.cs
│           ├── HoldFlowTests.cs
│           └── ConcurrencyHoldTests.cs   ← ★ বাধ্যতামূলক: একসাথে অনেক hold → overbook নয়
│
└── frontend/                             ← নতুন (Vite + React 18 + TS recommended)
    ├── Dockerfile
    ├── package.json
    ├── vite.config.ts
    ├── .env.example                      ← VITE_API_BASE_URL=http://localhost:8080
    ├── index.html
    └── src/
        ├── main.tsx
        ├── App.tsx
        ├── api/
        │   └── client.ts                 ← fetch wrapper + X-Student-Id header
        ├── context/
        │   └── StudentContext.tsx        ← উপরের input → localStorage
        ├── pages/
        │   ├── CreateClassPage.tsx       ← admin: name + totalSeats
        │   ├── ClassListPage.tsx         ← seats + "আবেদন করুন" + polling
        │   ├── HoldDetailPage.tsx        ← countdown, Pay, Cancel
        │   └── MyHoldsPage.tsx           ← /me/holds
        ├── components/
        │   ├── StudentIdBar.tsx
        │   ├── SeatSummary.tsx
        │   ├── Countdown.tsx
        │   └── ErrorBanner.tsx
        └── types/
            └── admission.ts
```

### Template modules কী করবে?

| Module | Recommendation |
|--------|----------------|
| **Admissions** | নতুন — সব assignment API এখানে |
| **Identity / Users / Roles** | Assignment-এ login নেই। **DI থেকে unregister** করে রাখো, অথবা solution থেকে সরিয়ে slim করো — graders confuse হবে না যদি unused endpoint expose না করো |
| **Notifications / AuditLogs** | Bonus নয়; সময় থাকলে audit রাখতে পারো |
| **BuildingBlocks** | রাখো — `Result`, `AppException`, `IDateTimeProvider`, soft-delete base ইত্যাদি কাজে লাগবে |

---

## ৪) Domain Model (প্রস্তাবিত)

### `AdmissionClass`

| Field | Meaning |
|-------|---------|
| `Id` | string/Guid — response-এ `id` |
| `Name` | e.g. `Class 6 - Morning Shift` |
| `TotalSeats` | capacity |

Seat counts **derived** (query-time) অথবা **denormalized counters** (transaction-এ আপডেট) — দুটোই OK।  
Concurrency-safe করতে denormalized counter + DB constraint/lock বেশি নিরাপদ।

```
availableSeats = totalSeats - heldSeats - confirmedSeats
```

### `SeatHold`

| Field | Meaning |
|-------|---------|
| `Id` | `holdId` |
| `ClassId` | FK |
| `StudentId` | 17-digit string (`X-Student-Id`) |
| `Status` | HELD / CONFIRMED / EXPIRED / CANCELLED |
| `ExpiresAt` | UTC (HELD-এর জন্য) |
| `CreatedAt` / `ConfirmedAt` | audit |

### Status transitions

```
(none) --apply--> HELD --confirm--> CONFIRMED
                 |--cancel--> CANCELLED
                 |--timeout--> EXPIRED   (worker বা lazy check)
```

---

## ৫) Concurrency Design (সবচেয়ে গুরুত্বপূর্ণ অংশ)

Assignment স্পষ্ট করেছে: হাজার অভিভাবক একসাথে + এমনকি **২টা API process** একই DB-তে।

### Recommended approach (PostgreSQL)

**Hold create (`POST .../holds`) এক transaction-এ:**

1. `SELECT … FROM admissions.classes WHERE id = … FOR UPDATE`  
   (row lock → দুই process-ও serialize হবে)
2. Count active: `status = HELD AND expires_at > now()` + `status = CONFIRMED`  
   অথবা locked counter columns পড়ো
3. `active >= totalSeats` হলে → **409**
4. একই student-এর ওই class-এ already active HELD/CONFIRMED থাকলে → **409** (বা যুক্তিসঙ্গত 4xx — `ASSUMPTIONS.md`-এ লেখো)
5. Insert hold (`HELD`, `expiresAt = now + HOLD_DURATION_SECONDS`)
6. Commit

**Extra safety (দুই process guarantee):**

- Partial unique index: এক student + এক class-এ শুধু একটা active row  
  `(student_id, class_id) WHERE status IN ('HELD','CONFIRMED')`
- Optional: `CHECK (held_seats + confirmed_seats <= total_seats)` যদি counter রাখো

**Confirm:**  
শুধু `status = HELD` এবং `expires_at > now()` এবং `student_id` match → update to `CONFIRMED`।  
Already `CONFIRMED` হলে same response (idempotent) — counter আর বাড়াবে না।

**Expire (API call ছাড়া):**

| Option | Pros | Cons |
|--------|------|------|
| **A. Background worker** (`IHostedService` প্রতি N সেকেন্ডে HELD→EXPIRED) | Seat দ্রুত মুক্ত | Clock/worker lag |
| **B. Lazy expire** (প্রতি read/write-এ `expires_at < now` দেখে EXPIRED ধরা) | সহজ | Count query-তে সবসময় filter লাগে |
| **C. A + B** | সবচেয়ে নিরাপদ | একটু বেশি কোড |

**Recommendation:** **C** — worker মুক্ত করে; সব seat-count query-তেও  
`HELD AND expires_at > UtcNow` ধরো যাতে worker দেরি করলেও overbook না হয়।

এই পুরো গল্প `ASSUMPTIONS.md`-এ লিখবে।

---

## ৬) Layer-wise কোথায় কী লিখবে (Template নিয়ম)

একই প্যাটার্ন যা `WHERE-TO-CODE.md`-এ আছে:

| কাজ | ফাইল |
|-----|------|
| Entity + status transition methods | `Modules/Admissions/Domain/Entities` |
| Use-cases (create class, hold, confirm…) | `Application/Services` |
| JSON contract DTOs | `Application/DTOs` |
| FluentValidation (name, totalSeats > 0, studentId 17 digit) | `Application/Validators` |
| DbContext, configs, migrations | `Infrastructure/Persistence` |
| Expiration worker | `Infrastructure/Background` |
| HTTP endpoints | `Presentation/Controllers` — routes **without** `api/v1` |
| `X-Student-Id` | `Api/Middleware` + `IStudentContext` |
| Module register | `Admissions/.../DependencyInjection` + `Api/Program.cs` |
| Tests | `backend/tests/...` |

### Controller sketch (route মনে রাখার জন্য)

```csharp
[ApiController]
[Route("classes")]
public sealed class ClassesController(ClassService classes, HoldService holds) : ControllerBase
{
    [HttpPost]           // POST /classes
    [HttpGet]            // GET /classes
    [HttpGet("{classId}")]
    [HttpPost("{classId}/holds")]
    ...
}

[Route("holds")]
public sealed class HoldsController(...) { ... }   // GET/POST confirm/DELETE

[Route("me")]
public sealed class MeController(...) { ... }      // GET /me/holds
```

---

## ৭) Frontend Structure & Screens

Vite + React 18 + TypeScript (assignment recommend)। UI library optional (plain CSS / MUI / whatever — সুন্দর লাগবে না, কাজ করতে হবে)।

| Screen | API | Notes |
|--------|-----|-------|
| Top bar: জন্ম নিবন্ধন নম্বর | — | `localStorage`-এ রাখো যাতে refresh-এও থাকে |
| Class তৈরি (admin) | `POST /classes` | name + totalSeats form |
| Class তালিকা | `GET /classes` | polling ~3–5s; Apply button |
| আবেদন detail | `GET /holds/{id}` | countdown → expire UI; Pay → `POST .../confirm`; Cancel → `DELETE` |
| আমার আবেদন | `GET /me/holds` | সব status |

Env: `VITE_API_BASE_URL=http://localhost:8080` (Docker-এ host থেকে backend hit)।

---

## ৮) Docker Compose (final shape)

Root `docker-compose.yml` যেন শুধু Docker দিয়ে চলে:

```yaml
services:
  postgres:
    # volume: pgdata  ← persistence বাধ্যতামূলক
  backend:
    build: ./backend
    ports: ["8080:8080"]
    environment:
      ConnectionStrings__DefaultConnection: ...
      HOLD_DURATION_SECONDS: "600"
  frontend:
    build: ./frontend
    ports: ["3000:80"]   # বা Vite preview; README-তে URL লিখবে
    environment:
      VITE_API_BASE_URL: http://localhost:8080
```

- Backend **must** listen on **8080**।  
- Redis এই assignment-এ বাধ্যতামূলক নয় — সরালে compose সরল হবে; রাখলেও ক্ষতি নেই যদি `docker compose up` এ বাড়তি dependency ঝামেলা না করে।  
- `HOLD_DURATION_SECONDS` env দিয়ে config করো।

---

## ৯) Implementation Order (কোড শুরু করার ধাপ)

পরের session-এ এই ক্রমে কোড করো — কম ঝুঁকি, grader-friendly:

### Phase A — Repo layout
1. বর্তমান কোড `backend/`-এ সরানো (solution paths ঠিক করা)।
2. Root-এ নতুন `docker-compose.yml` (backend path update)।
3. `frontend/` Vite scaffold।

### Phase B — Admissions module (backend)
1. Domain entities + enums।
2. DbContext + configurations + unique/partial indexes।
3. `ClassService` + `HoldService` (transaction + `FOR UPDATE`)。
4. Controllers with **exact** routes/status codes।
5. `StudentIdMiddleware` + validation (17 digit)।
6. Expiration worker + lazy expire in queries।
7. `HOLD_DURATION_SECONDS` config।
8. Seed কিছু দরকার নেই — admin UI দিয়ে class তৈরি হবে।

### Phase C — Trim unused auth surface
1. Assignment demo-তে JWT endpoints expose না করো (বা Program.cs থেকে Identity/Users/Roles register বাদ)।
2. CORS-এ frontend origin allow করো।

### Phase D — Tests (≥3, একটা concurrency)
1. Create class + list seats।
2. Hold → confirm happy path + idempotent confirm।
3. **Concurrency:** `totalSeats = 10` → 50 parallel hold requests → successful holds ≤ 10।  
   (ঐচ্ছিক আরও শক্ত: দুই `WebApplicationFactory` / দুই process এক DB — সময় থাকলে)।

### Phase E — Frontend
1. StudentId bar + routing।
2. ৪টা screen + polling + countdown।
3. Human-readable errors।

### Phase F — Docs & submit polish
1. `README.md` — `docker compose up`, URLs, test commands।
2. `ASSUMPTIONS.md` — hold duration UX, error codes for duplicate apply, locking strategy, 2-process guarantee।
3. `AI_USAGE.md`।
4. ছোট ছোট commit history (একটা বড় dump commit নয়)।
5. Public GitHub + submission link।  
   **PDF share/public করবে না।**

---

## ১০) ASSUMPTIONS.md-এ যা লিখবে বলে আগে থেকে ঠিক রাখো

নিচের সিদ্ধান্তগুলো কোডের আগে ধরে নাও (পরে doc-এ কারণসহ):

1. Hold duration default 600s; test-এ ছোট মান (যেমন 5–10s) env দিয়ে।
2. Duplicate active apply → `409` (বা `400`) + clear message।
3. Invalid/missing `X-Student-Id` → `400`।
4. Confirm অন্যের hold / expired → `403`/`409`/`404` — যেটা বেছে নেবে একভাবে রাখো।
5. Seat counts: HELD মানে শুধু `expires_at > UtcNow`।
6. Overbook prevention: `SELECT FOR UPDATE` + partial unique index (+ optional check constraint)।
7. Two-process safety: DB locks/constraints — in-memory lock **নয়**।
8. Admin vs parent আলাদা auth নেই — যে কেউ UI থেকে class তৈরি করতে পারবে (spec login চায় না)।
9. ID format: Guid বা `c_`/`h_` prefix — যেটাই হোক consistent।
10. Time: সব expiry **UTC** (`expiresAt` ISO-8601 Z)।

---

## ১১) এখনকার Template থেকে কী reuse করবে / কী ছুঁবে না

### Reuse
- `BuildingBlocks` (`Result`, exceptions, `AuditableEntity`, DI patterns)
- Module ৪-layer + per-module DbContext/schema
- `ExceptionHandlingMiddleware`, correlation id
- EF migration script pattern
- xUnit IntegrationTests + `WebApplicationFactory`
- Docker/Postgres volume অভ্যাস

### Change / Avoid for this assignment
- `/api/v1` prefix on admission endpoints
- JWT login requirement for admission flows
- Wrapping admission responses in non-contract envelopes (যদি grader strict হয়)
- In-memory seat tracking only (restart/2-process fail করবে)
- Redis-as-only source of truth for holds

---

## ১২) Success Checklist (জমা দেওয়ার আগে)

- [ ] `docker compose up` → Postgres + backend `:8080` + frontend URL
- [ ] ৮টা API contract field/status code মিলে
- [ ] Concurrent hold overbook করে না (test আছে)
- [ ] Hold auto-expire হয়; UI countdown/Pay disable করে
- [ ] Seat list poll করে
- [ ] Student id persist → `/me/holds` কাজ করে
- [ ] Data volume persist after compose down/up
- [ ] `README.md`, `ASSUMPTIONS.md`, `AI_USAGE.md`
- [ ] ≥3 backend automated tests (1 concurrency)
- [ ] Commit history অর্থপূর্ণ
- [ ] Assignment PDF public/share করা হয়নি

---

## ১৩) পরবর্তী পদক্ষেপ

এই MD পড়ার পর কোডিং শুরু করলে প্রথমে বলো — আমি **Phase A (repo → `backend/` + `frontend/` scaffold)** থেকে implement শুরু করতে পারি, তারপর **Admissions module**।

সংশ্লিষ্ট বিদ্যমান গাইড:
- [`WHERE-TO-CODE.md`](../WHERE-TO-CODE.md) — কোথায় কী লিখবে
- [`CODE-STRUCTURE.md`](../CODE-STRUCTURE.md) — বর্তমান ফাইল ট্রি
- [`docs/FOLDER-STRUCTURE-BANGLA.md`](FOLDER-STRUCTURE-BANGLA.md) — architecture ব্যাখ্যা
