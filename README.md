![Status](https://img.shields.io/badge/Status-Active-success)
![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8-5C2D91?logo=dotnet&logoColor=white)
![Next.js](https://img.shields.io/badge/Next.js-16-000000?logo=nextdotjs&logoColor=white)
![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black)
![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?logo=typescript&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?logo=microsoftsqlserver&logoColor=white)

# SEAL.NET — Competition Management Platform

A full-stack competition management platform built with **ASP.NET Core 8 Web API** and **Next.js 16**, designed to manage the complete lifecycle of hackathons, academic competitions, and judging workflows.

This project demonstrates production-oriented full-stack engineering, including authentication, role-based authorization, workflow automation, audit logging, containerized deployment, and modern frontend architecture.

---

## Overview

SEAL.NET is a decoupled web application that enables organizations to run technical competitions efficiently.

The platform supports:

- participant registration
- admin approval workflows
- team formation and management
- event / category / round configuration
- judge assignment
- project submissions
- scoring and evaluation
- ranking generation
- audit trail tracking

Built as a portfolio-grade project to showcase backend architecture, frontend engineering, API design, and deployment practices.

---

## Live Architecture

```text
Frontend (Next.js 16)
        ↓
REST API (ASP.NET Core 8)
        ↓
Business Services
        ↓
Repository Layer
        ↓
Entity Framework Core
        ↓
SQL Server
```

---

## Core Features

### Authentication & Authorization

- JWT authentication
- HttpOnly cookie token storage
- ASP.NET Core Identity integration
- Role-based access control (RBAC)
- secure login/logout flow
- lockout protection against brute-force login attempts

Supported roles:

| Role | Responsibilities |
|------|------------------|
| Admin | Full system management |
| Judge | Review assigned submissions and submit scores |
| Member | Register and participate in teams |
| TeamLeader | Submit projects on behalf of teams |
| Mentor | Reserved for future implementation |

---

### Competition Management

Admins can:

- create and manage events
- configure competition categories
- create competition rounds
- define scoring criteria
- assign judges to rounds/categories
- approve or reject participants
- manage teams
- eliminate teams with reasons
- monitor rankings
- review score audit history

---

### Team Management

Participants can:

- register accounts
- create teams
- join teams
- manage team membership
- submit project URLs
- participate across competition rounds

---

### Judge Workspace

Judges can:

- view assigned submissions
- review project entries
- submit scores
- bulk score evaluations
- provide comments and evaluation feedback

---

### Ranking System

Built-in ranking features:

- public leaderboard
- admin leaderboard
- round-based ranking
- category-based ranking
- score aggregation

---

### Audit & Monitoring

Operational visibility includes:

- score audit logs
- exception handling middleware
- health monitoring endpoint
- structured API error responses

---

## Tech Stack

### Backend

**Platform**
- .NET 8
- ASP.NET Core Web API

**Architecture**
- Repository Pattern
- DTO Pattern
- Dependency Injection
- Service Layer Separation

**Authentication**
- ASP.NET Core Identity
- JWT Bearer Authentication

**Database**
- SQL Server
- Entity Framework Core 8

**Documentation**
- Swagger / OpenAPI

**Validation**
- Data Annotations
- FluentValidation

**Utilities**
- AutoMapper
- BCrypt

---

### Frontend

**Framework**
- Next.js 16 (App Router)
- React 19
- TypeScript

**UI**
- Tailwind CSS 4
- Radix UI
- Lucide Icons

**Forms & Validation**
- React Hook Form
- Zod

**State Management**
- TanStack React Query

**Networking**
- Axios

**UX**
- Sonner Toast Notifications

---

### Tooling

- Docker
- Docker Compose
- GitHub Actions
- xUnit Testing
- EF Core InMemory Testing

---

## Project Structure

```bash
SEAL.NET/
│
├── backend/
│   ├── Controllers/
│   ├── DTOs/
│   ├── Models/
│   ├── Repositories/
│   ├── Services/
│   ├── Middleware/
│   └── Data/
│
├── frontend/
│   ├── src/app/
│   ├── src/components/
│   ├── src/services/
│   ├── src/hooks/
│   └── src/types/
│
├── tests/
│
├── docker-compose.yml
└── README.md
```

---

## Getting Started

## Prerequisites

Install:

- .NET 8 SDK
- Node.js 18+
- SQL Server
- Docker (optional)

---

## Local Development

### Clone Repository

```bash
git clone https://github.com/hbphucc/SEAL.NET.git
cd SEAL.NET
```

---

### Backend Setup

```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run
```

Backend runs at:

```bash
https://localhost:5001
```

Swagger:

```bash
https://localhost:5001/swagger
```

---

### Frontend Setup

```bash
cd frontend
npm install
npm run dev
```

Frontend runs at:

```bash
https://localhost:3000
```

---

## Docker Deployment

Set environment variables:

```bash
JWT_KEY=your-secure-secret-key
MSSQL_SA_PASSWORD=YourStrongPassword123!
```

Run:

```bash
docker compose up --build
```

---

## API Highlights

Representative endpoints:

### Authentication

```http
POST /api/auth/register
POST /api/auth/login
POST /api/auth/logout
GET /api/auth/me
```

### Team Management

```http
POST /api/teams
GET /api/teams/my-team
POST /api/teams/add-member
DELETE /api/teams/remove-member
```

### Admin

```http
GET /api/admin/users
PUT /api/admin/users/{id}/approve
PUT /api/admin/teams/{id}/approve
PUT /api/admin/teams/{id}/eliminate
```

### Judge

```http
GET /api/judge/scores/my-assigned-submissions
POST /api/judge/scores
POST /api/judge/scores/bulk
```

---

## Security Practices

Implemented security measures:

- HttpOnly auth cookies
- JWT validation
- RBAC authorization
- login lockout policy
- secure CORS configuration
- centralized exception middleware
- production-safe error handling
- HTTPS support

---

## Testing

Backend test suite:

```bash
cd tests
dotnet test
```

Testing includes:

- service layer validation
- repository behavior
- API business logic

---

## Production Readiness

Implemented:

- containerized deployment
- health checks
- structured API responses
- secret externalization
- environment-based configuration

Recommended future improvements:

- refresh token rotation
- Redis caching
- distributed logging
- background jobs
- message queue integration
- rate limiting
- CI/CD deployment pipeline
- observability dashboards

---

## License

This project is developed for educational and portfolio purposes.
