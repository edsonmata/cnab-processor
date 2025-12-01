# CNAB Processor

Full-stack CNAB processor: upload fixed-width CNAB files, parse them, import into SQL Server, and explore transactions and store balances via REST API and React UI.

## Features
- CNAB upload (.txt) with validation and parsing
- Optimized bulk import and full pagination
- Store balances and statistics endpoints
- JWT authentication (bonus) with protected routes
- React UI for upload, listing, and dashboard
- Swagger docs, Docker Compose, and automated tests

## Tech Stack
- Backend: .NET 8, ASP.NET Core Web API, EF Core, SQL Server, Serilog, xUnit
- Frontend: React 18 + Vite, vanilla CSS, Fetch API
- Ops: Docker Compose

## Architecture
Clean Architecture: Api (controllers), Domain (entities/services), Infrastructure (EF Core repositories), Frontend (React).

## Prerequisites
- Docker and Docker Compose
- Optional: .NET 8 SDK, Node.js 20+ if running without Docker

## Quick Start (Docker)
1. `docker-compose up --build`
2. Wait ~60 seconds for SQL Server to finish initializing.
3. Frontend: http://localhost:3000
4. API: http://localhost:5099 (Swagger at /swagger, health at /api/health)
5. Demo login: admin / Admin@123 (or user / User@123)
6. Upload the sample `CNAB.txt` in the repo to import transactions.

## Local Development (without Docker)
- Backend: `cd backend/src/CnabProcessor.Api && dotnet run` (http://localhost:5000)
- Frontend: `cd frontend && npm install && npm run dev` (http://localhost:5173)
- Database: use SQL Server 2022 and set `ConnectionStrings__DefaultConnection`

## Tests
- All tests: `cd backend && dotnet test`
- Unit only: `cd backend/tests/CnabProcessor.UnitTests && dotnet test`
- Integration only: `cd backend/tests/CnabProcessor.IntegrationTests && dotnet test`
- Coverage: `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover`

## API
Base URL: `http://localhost:5099/api`

Key endpoints: `/cnab/upload`, `/cnab/transactions`, `/cnab/store/{storeName}`, `/cnab/balances`, `/cnab/stats`, `/health`.
Auth: JWT Bearer (bonus). See `AUTHENTICATION_GUIDE.md` and `API_GUIDE.md` for details.

## Environment Variables
- Backend: `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`, `ConnectionStrings__DefaultConnection`
- Frontend: `VITE_API_URL`
- Database (docker): `SA_PASSWORD`, `ACCEPT_EULA`, `MSSQL_PID`

See `docker-compose.yml` for defaults.

## Documentation
- `QUICK_START.md`
- `API_GUIDE.md`
- `AUTHENTICATION_GUIDE.md`
- `TECH_STACK.md`
- `PERFORMANCE_OPTIMIZATION.md`

