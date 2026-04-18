# PROHUB Manager

Multi-tenant project management for SMBs. Built with ASP.NET Core 8 (Minimal APIs) + React + PostgreSQL.

---

## Environment variables

| Variable | Required | Description |
|---|---|---|
| `ConnectionStrings__Prohub` | Yes | Full Npgsql connection string |
| `ASPNETCORE_ENVIRONMENT` | No | `Development` (local) / `Production` (Railway) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | No | OpenTelemetry collector endpoint |

### Railway (production)
Set `ConnectionStrings__Prohub` in the Railway service environment variables:
```
Host=postgres.railway.internal;Port=5432;Database=railway;Username=postgres;Password=***;SSL Mode=Require;Trust Server Certificate=true;
```

---

## Running locally

### Prerequisites
- .NET 8 SDK
- Docker (for Postgres) OR a local Postgres instance
- Node.js 20+

### 1. Start Postgres with Docker
```bash
docker run -d --name prohub-pg \
  -e POSTGRES_DB=prohub \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:16
```

### 2. Run the backend
```powershell
cd backend\PROHUB.API\PROHUB.API.Server
dotnet run
```
The `appsettings.Development.json` already points to `localhost:5432`.
Override the connection string with an env var if needed:
```powershell
$env:ConnectionStrings__Prohub = "Host=localhost;Port=5432;Database=prohub;Username=postgres;Password=postgres"
dotnet run
```

### 3. Run the frontend (dev mode)
```powershell
cd backend\PROHUB.API\frontend
npm install
npm run dev
```

### 4. (Optional) Run everything via .NET Aspire AppHost
```powershell
cd backend\PROHUB.API\PROHUB.API.AppHost
dotnet run
```

---

## Verification checklist

| URL | Expected |
|---|---|
| `http://localhost:5534/api/health` | `{"ok":true,"data":{"status":"healthy",...}}` |
| `http://localhost:5534/swagger` | Swagger UI with all endpoints |
| `POST /api/companies` body `{"name":"Acme"}` | Company created |
| `GET /api/companies` | List of companies |

---

## DB schema notes

EF Core uses `UseSnakeCaseNamingConvention()` from `EFCore.NamingConventions`.
C# property `CreatedAtUtc` maps to column `created_at_utc`, `CompanyId` → `company_id`, etc.

**If your Railway schema uses different column names** (e.g., `created_at` instead of `created_at_utc`),
add explicit `[Column("created_at")]` attributes to the entity properties in `Entities/`.

---

## Railway deployment

1. Connect your GitHub repo in Railway.
2. Set `ConnectionStrings__Prohub` env var.
3. Railway will detect the `Dockerfile` at repo root and build automatically.
4. The app serves the React frontend from `/` and the API from `/api/`.
5. Swagger is available at `/swagger`.

---

## Project structure (backend)

```
PROHUB.API.Server/
  Common/          ApiResponse, AppErrors
  Data/            ProhubDbContext (EF Core)
  Entities/        One file per DB table
  Dtos/            Request/response records
  Endpoints/       Minimal API route groups
  Program.cs       App bootstrap
```
