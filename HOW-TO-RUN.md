# How to Run This Project

এই গাইড দিয়ে Modular Monolith API লোকালে চালাবে।  
দুইভাবে চালানো যায়: **Docker (সহজ)** অথবা **dotnet run (ডেভেলপমেন্ট)**.

---

## Prerequisites

আগে ইনস্টল থাকতে হবে:

| Tool | কেন লাগে |
|------|----------|
| [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) | API বিল্ড ও রান |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Postgres + Redis + (ঐচ্ছিক) পুরো API |
| PowerShell / Terminal | কমান্ড চালাতে |

চেক:

```bash
dotnet --version
docker --version
docker compose version
```

`.NET` version `9.x` হওয়া উচিত।

---

## Option A — সবকিছু Docker দিয়ে (সবচেয়ে সহজ)

Postgres + Redis + API একসাথে উঠবে।

### 1) Env ফাইল তৈরি

প্রজেক্ট রুটে:

```bash
cp .env.example .env
```

Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

`.env` খুলে অন্তত `JWT_SECRET` বদলাও (কমপক্ষে **32 character**):

```env
JWT_SECRET=CHANGE_ME_TO_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS!
```

### 2) চালু করো

প্রজেক্ট রুট থেকে:

```bash
docker compose up --build
```

Background-এ চালাতে:

```bash
docker compose up --build -d
```

### 3) URL গুলো

| কী | URL |
|----|-----|
| API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |
| Health | http://localhost:8080/health |

### 4) বন্ধ করা

```bash
docker compose down
```

ডাটাও মুছতে চাইলে (volume wipe):

```bash
docker compose down -v
```

---

## Option B — লোকাল `dotnet run` (কোড চেঞ্জ করে ডেভ)

API তোমার মেশিনে চলবে; শুধু DB/Redis Docker-এ।

### 1) Postgres (+ Redis) চালু

```bash
docker compose up postgres redis -d
```

### 2) Connection / JWT সেটআপ

`src/Api/appsettings.Development.json`-এ ডিফল্ট আছে:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=modular_monolith_dev;Username=postgres;Password=postgres"
}
```

Docker Postgres default (`POSTGRES_DB=modular_monolith`) ব্যবহার করতে চাইলে connection string-এ Database নাম মিলিয়ে নাও, অথবা user-secrets ব্যবহার করো:

```bash
dotnet user-secrets init --project src/Api/Api.csproj

dotnet user-secrets set "Jwt:Secret" "YOUR_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS" --project src/Api/Api.csproj

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=modular_monolith;Username=postgres;Password=postgres" --project src/Api/Api.csproj
```

### 3) API রান

```bash
dotnet run --project src/Api/Api.csproj
```

অথবা HTTPS প্রোফাইল:

```bash
dotnet run --project src/Api/Api.csproj --launch-profile https
```

### 4) URL গুলো (লোকাল)

| কী | URL |
|----|-----|
| HTTP | http://localhost:5180 |
| HTTPS | https://localhost:7025 |
| Swagger | http://localhost:5180/swagger |
| Health | http://localhost:5180/health |

> Development-এ startup-এ migration apply হয় এবং `Admin` / `User` role seed হয়।

---

## প্রথমবার টেস্ট (Swagger)

1. Browser-এ Swagger খোলো  
2. `POST /api/v1/auth/register` দিয়ে ইউজার বানাও  
3. `POST /api/v1/auth/login` দিয়ে `accessToken` নাও  
4. Swagger-এ **Authorize** → `Bearer <accessToken>`  
5. অন্য endpoint টেস্ট করো (যেমন `GET /api/v1/users/me`)

---

## Useful commands

### Build / Test

```bash
dotnet build ModularMonolith.sln
dotnet test ModularMonolith.sln
```

### নতুন migration

```powershell
./scripts/add-migration.ps1 -Module Identity -Name AddSomething
```

Module নাম: `Identity`, `Users`, `Roles`, `Notifications`, `AuditLogs`

### Redis চালু রাখতে চাইলে (লোকাল API)

`appsettings.Development.json`:

```json
"Cache": {
  "UseRedis": true,
  "RedisConnection": "localhost:6379"
}
```

Docker Compose-এ Redis ইতিমধ্যে চলমান থাকতে হবে।

### Logs দেখা (Docker)

```bash
docker compose logs -f api
```

---

## Common problems

| সমস্যা | সমাধান |
|--------|--------|
| Port 8080 / 5432 already in use | `.env`-এ `API_PORT` / `POSTGRES_PORT` বদলাও, অথবা অন্য প্রসেস বন্ধ করো |
| JWT secret too short | `.env` বা user-secrets-এ কমপক্ষে 32-char secret দাও |
| Cannot connect to Postgres | আগে `docker compose up postgres -d` চালাও; connection string-এ `Host=localhost` (লোকাল) বা `Host=postgres` (API container) |
| Swagger দেখা যাচ্ছে না | `ASPNETCORE_ENVIRONMENT=Development` আছে কিনা চেক করো |
| Migration error | DB healthy কিনা দেখো; দরকার হলে `docker compose down -v` করে ফ্রেশ স্টার্ট |

---

## Quick cheat sheet

```bash
# Docker — সব একসাথে
cp .env.example .env
docker compose up --build

# লোকাল ডেভ
docker compose up postgres redis -d
dotnet run --project src/Api/Api.csproj
```

| Mode | API URL | Swagger |
|------|---------|---------|
| Docker | http://localhost:8080 | http://localhost:8080/swagger |
| Local | http://localhost:5180 | http://localhost:5180/swagger |

আরও আর্কিটেকচার/কোড লোকেশন: [`CODE-STRUCTURE.md`](CODE-STRUCTURE.md), [`WHERE-TO-CODE.md`](WHERE-TO-CODE.md), [`README.md`](README.md)
