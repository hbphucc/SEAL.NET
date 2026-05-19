# SEAL.NET

SEAL.NET is a competition team-management system with an ASP.NET Core backend and a Next.js frontend. It supports account approval, team registration, event/category/round setup, judge assignments, project submissions, scoring, ranking, and score audit logs.

## Tech Stack

- Backend: ASP.NET Core 8, Entity Framework Core, SQL Server, ASP.NET Identity, JWT bearer auth via HttpOnly cookie
- Frontend: Next.js 16 App Router, React 19, TypeScript, Tailwind CSS 4, React Query, Axios
- Tests: xUnit with EF Core InMemory
- Tooling: Docker, GitHub Actions CI

## Required Configuration

Use environment variables or .NET user-secrets for sensitive values. Do not commit real secrets.

Backend:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=SEALDB;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "replace-with-at-least-32-random-characters"
dotnet user-secrets set "Jwt:Issuer" "SEAL.API"
dotnet user-secrets set "Jwt:Audience" "SEAL.API.Users"
dotnet user-secrets set "Jwt:ExpireDays" "7"
```

Optional local admin bootstrap:

```powershell
dotnet user-secrets set "AdminBootstrap:Enabled" "true"
dotnet user-secrets set "AdminBootstrap:Email" "admin@example.com"
dotnet user-secrets set "AdminBootstrap:Password" "replace-with-a-strong-password"
```

Frontend:

```env
NEXT_PUBLIC_API_BASE_URL=https://localhost:5001/api
```

## Run Locally

Backend:

```powershell
dotnet restore
dotnet ef database update
dotnet run
```

Frontend:

```powershell
cd frontend
npm install
npm run dev
```

Default local URLs:

- Backend API: `https://localhost:5001/api`
- Swagger in development: `https://localhost:5001/swagger`
- Frontend: `https://localhost:3000`
- Health check: `https://localhost:5001/health`

## Tests and Builds

```powershell
dotnet build
dotnet test
cd frontend
npm run lint
npm run build
```

## Docker

Set a strong JWT key before running compose:

```powershell
$env:JWT_KEY="replace-with-at-least-32-random-characters"
$env:MSSQL_SA_PASSWORD="Change_this_password_123!"
docker compose up --build
```

Compose starts SQL Server, backend, and frontend. Apply migrations before real use.

## Roles

- `Admin`: manages users, events, rounds, categories, teams, rankings, judge assignments, and score audits
- `Judge`: reviews assigned submissions and submits scores
- `Member`: registers and joins teams
- `TeamLeader`: submits project URLs for an approved team
- `Mentor`: reserved for future workflows

## Main Features

- Account registration and admin approval
- Role management
- Competition event setup with categories, rounds, and criteria
- Team creation, approval, elimination, and deletion
- Judge assignment management
- Judge scoring workspace
- Project submissions
- Public and admin rankings
- Score audit logs

## Production Notes

- Store secrets in environment variables, user-secrets, or a secret manager.
- Keep `Cors:AllowedOrigins` strict when credentials are enabled.
- Use HTTPS in production.
- Run EF migrations during deployment.
- Monitor `/health` from your hosting platform.
