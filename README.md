# School Admission System

Online school admission (first-come-first-served) with seat holds, fee confirmation, React UI, PostgreSQL, and Docker.

Stack: **.NET 9 · PostgreSQL · React 18+ (Vite) · Docker**

## Quick start

```bash
cp .env.example .env   # PowerShell: Copy-Item .env.example .env
docker compose up --build
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| Backend API | http://localhost:8080 |
| Health | http://localhost:8080/health |
| Swagger (Dev) | http://localhost:8080/swagger |

Stop: `docker compose down`  
Wipe DB volume: `docker compose down -v`

Data persists across `down`/`up` via the `pgdata` volume.

## How to demo in the UI

1. Open http://localhost:3000  
2. Enter a **17-digit** birth registration number in the top bar (saved in `localStorage`).  
3. **Create class** → e.g. `Class 6 - Morning Shift`, seats `120`.  
4. **Classes** → **আবেদন করুন** → opens hold detail with countdown.  
5. **ভর্তি ফি দিন** confirms the seat (no real payment gateway).  
6. **My applications** lists all holds for that student id.

Seat lists and hold status poll every few seconds. When a hold expires, the UI shows `EXPIRED` and disables Pay/Cancel without a full page reload.

## API contract

Base URL: `http://localhost:8080`  
Student identity header: `X-Student-Id` (17 digits)

| Method | Path | Notes |
|--------|------|-------|
| `POST` | `/classes` | `{ "name", "totalSeats" }` → 201 |
| `GET` | `/classes` | seat summary list |
| `GET` | `/classes/{classId}` | one class |
| `POST` | `/classes/{classId}/holds` | create hold → 201 / 409 |
| `GET` | `/holds/{holdId}` | `HELD` \| `CONFIRMED` \| `EXPIRED` \| `CANCELLED` |
| `POST` | `/holds/{holdId}/confirm` | idempotent confirm |
| `DELETE` | `/holds/{holdId}` | cancel → 204 |
| `GET` | `/me/holds` | current student’s applications |

Hold duration: env `HOLD_DURATION_SECONDS` (default **600**).

## Local development (without full Docker app)

```bash
docker compose up postgres -d
dotnet run --project backend/src/Api/Api.csproj
cd frontend && npm install && npm run dev
```

Set `VITE_API_BASE_URL=http://localhost:8080` for the Vite app.  
API local HTTP profile may use another port — prefer Docker backend on **8080** for contract testing.

## Tests

```bash
dotnet test backend/ModularMonolith.sln
```

Includes:

- Domain unit tests for hold transitions  
- Class create/list integration test  
- Hold → confirm (idempotent) + cancel flow  
- **Concurrency:** 50 parallel holds against 10 seats → exactly 10 created, no overbook  

Integration tests use **Testcontainers** (Docker required).

## Project layout

```
backend/     ASP.NET Modular Monolith + Admissions module
frontend/    Vite + React + TypeScript UI
docker-compose.yml
ASSUMPTIONS.md
AI_USAGE.md
```

## Related docs

- [`ASSUMPTIONS.md`](ASSUMPTIONS.md) — locking, error codes, expiry strategy  
- [`AI_USAGE.md`](AI_USAGE.md) — AI assistance disclosure  
- [`docs/SCHOOL-ADMISSION-APPLY-PLAN.md`](docs/SCHOOL-ADMISSION-APPLY-PLAN.md) — implementation plan
