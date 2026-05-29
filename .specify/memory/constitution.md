<!--
Sync Impact Report
Version change: template -> 1.0.0
Modified principles:
- Template principle 1 -> I. Domain Workflow Integrity
- Template principle 2 -> II. Security and Role Boundaries
- Template principle 3 -> III. API Contract Discipline
- Template principle 4 -> IV. Testable Business Behavior
- Template principle 5 -> V. Observability and Auditability
Added sections:
- Technology and Architecture Standards
- Delivery Workflow and Quality Gates
Removed sections:
- None
Templates requiring updates:
- UPDATED .specify/templates/plan-template.md
- UPDATED .specify/templates/spec-template.md
- UPDATED .specify/templates/tasks-template.md
- PENDING .specify/templates/commands/*.md not present in this repository
- UPDATED README.md reviewed; no principle references required updates
Follow-up TODOs:
- None
-->

# SEAL.NET Constitution

## Core Principles

### I. Domain Workflow Integrity
Competition lifecycle behavior MUST preserve the real workflow of events, categories,
rounds, teams, submissions, judging, scoring, ranking, approvals, eliminations, and
audit history. Features MUST define the affected roles, state transitions, failure
states, and ranking or scoring impact before implementation. Data changes that can
alter competition outcomes MUST be traceable and reversible through explicit domain
rules, not incidental UI behavior.

Rationale: SEAL.NET manages competitive outcomes, so unclear workflows or hidden
side effects can damage fairness and operator trust.

### II. Security and Role Boundaries
Every feature MUST define its authentication and authorization behavior for Admin,
Judge, Member, TeamLeader, and Mentor where applicable. Server-side authorization
MUST be authoritative; frontend route protection is only a user experience layer.
Secrets MUST stay outside source control, authentication tokens MUST use secure
storage patterns, and error responses MUST avoid leaking sensitive internals.

Rationale: The platform handles accounts, competition administration, submissions,
and scores, which require explicit access control and production-safe failure modes.

### III. API Contract Discipline
Backend features MUST expose stable REST contracts with DTOs, validation rules,
structured errors, and Swagger/OpenAPI visibility when externally callable. Frontend
code MUST consume API behavior through typed service boundaries rather than
duplicating backend rules in components. Contract changes MUST document compatibility
impact and any required frontend or client migration.

Rationale: SEAL.NET is a decoupled ASP.NET Core API and Next.js application; stable
contracts keep both halves deployable and reviewable.

### IV. Testable Business Behavior
Business logic changes MUST include automated tests at the lowest practical level,
with integration or API-level coverage for authorization, persistence, scoring,
ranking, approval, and submission workflows. Tests MAY be omitted only for
documentation-only or purely visual changes, and the omission MUST be recorded in
the implementation plan or task list. Existing relevant tests MUST pass before a
feature is considered complete.

Rationale: Competition workflows are stateful and role-sensitive; tests are the most
reliable way to prevent regressions in fairness, security, and ranking behavior.

### V. Observability and Auditability
Operationally significant actions MUST produce enough structured logging, audit
records, or health signals to diagnose failures without exposing secrets. Scoring,
ranking-affecting changes, participant approvals, team eliminations, authentication
failures, and unexpected exceptions MUST be observable from backend logs or persisted
audit records. New error paths MUST use centralized exception handling and
consistent response shapes.

Rationale: Administrators need defensible records for competition outcomes, and
operators need actionable signals when production behavior fails.

## Technology and Architecture Standards

SEAL.NET MUST remain a decoupled full-stack web application unless a plan explicitly
justifies a different shape. Backend work MUST target .NET 8, ASP.NET Core Web API,
Entity Framework Core, SQL Server, ASP.NET Core Identity, dependency injection,
service-layer separation, repositories where they already exist, DTOs, and
FluentValidation or data annotations for input validation. Backend tests MUST use
xUnit and fit the existing `Backend/Tests/SEAL.NET.Tests` project unless a plan
documents a more appropriate test location.

Frontend work MUST target Next.js App Router, React, TypeScript, Tailwind CSS,
Radix UI, TanStack React Query, Axios service modules, React Hook Form, and Zod
where forms or client validation are involved. Shared behavior MUST live in
services, hooks, contexts, or typed modules rather than being duplicated across
pages. Docker and environment-based configuration MUST remain the deployment
baseline for changes that affect runtime services.

## Delivery Workflow and Quality Gates

Each feature specification MUST prioritize independently testable user stories and
define measurable success criteria. Each implementation plan MUST identify impacted
roles, API contracts, data model changes, security boundaries, audit/logging needs,
test coverage, and deployment or configuration changes. Tasks MUST be grouped by
user story, include exact file paths, and include validation tasks for relevant
backend tests, frontend lint/build checks, API contracts, and quickstart behavior.

Complexity that adds new architectural layers, cross-service dependencies,
background processing, caching, or message queues MUST be justified in the plan with
a simpler alternative considered. Work is complete only when implemented behavior,
documentation, tests, and operational notes are consistent with this constitution.

## Governance

This constitution supersedes conflicting local practices for SEAL.NET feature
planning and implementation. Amendments MUST update this file, include a Sync
Impact Report, and review affected Spec Kit templates and runtime guidance.

Versioning follows semantic versioning:
- MAJOR: Removes or redefines existing principles or governance in a way that can
  invalidate previously compliant work.
- MINOR: Adds a principle, mandatory section, or materially expands compliance
  expectations.
- PATCH: Clarifies wording, fixes typos, or updates non-semantic guidance.

Every generated plan MUST pass the Constitution Check before design proceeds and
again after design artifacts are created. Reviews MUST call out any unresolved
constitutional violation and either require a fix or record an explicit, justified
exception in Complexity Tracking.

**Version**: 1.0.0 | **Ratified**: 2026-05-30 | **Last Amended**: 2026-05-30
