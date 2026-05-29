# Implementation Plan: TeamLeader Add Team Member

**Branch**: `001-add-team-member` | **Date**: 2026-05-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-add-team-member/spec.md`

## Summary

Enable a signed-in TeamLeader to add a student directly to the TeamLeader's own team by submitting only `StudentCode`, without exposing or requiring team/user GUID input in the user-facing workflow. The implementation will add a GUID-free backend endpoint (`POST /api/teams/my-team/members`) that resolves the current user's led team server-side, reuses and tightens existing team-member validation, returns an updated member/team representation, records audit events for successful and integrity-denied attempts, and updates the My Team frontend to use the direct add-member flow instead of the current invite panel.

## Technical Context

**Language/Version**: Backend: C# on .NET 8. Frontend: TypeScript with Next.js 16 App Router and React 19.

**Primary Dependencies**: Backend: ASP.NET Core Web API, EF Core 8, ASP.NET Core Identity, JWT bearer auth, Swashbuckle/OpenAPI, data annotations/FluentValidation package availability. Frontend: Next.js, React, TanStack React Query, Axios, Tailwind CSS, lucide-react, sonner.

**Storage**: SQL Server through EF Core in production; EF Core InMemory provider for existing tests.

**Testing**: Backend xUnit in `Backend/Tests/SEAL.NET.Tests`; frontend validation through `npm run lint` and `npm run build` from `Frontend/`.

**Target Platform**: Decoupled full-stack web application: ASP.NET Core API plus Next.js browser frontend.

**Project Type**: Web application with REST API backend and typed frontend service/hook boundaries.

**Performance Goals**: Valid add-member attempts should complete well within the existing UX success criterion of under 30 seconds; server-side lookup and validation should use indexed/keyed EF queries and avoid loading unrelated teams or users.

**Constraints**: User-facing add-member workflow must accept only `StudentCode`; no UserId, TeamId, or GUID field should be collected or displayed as add-member input. Server-side authorization is authoritative. No new infrastructure, background processing, cache, or architectural layer is required.

**Scale/Scope**: Single workflow on the My Team page plus one REST endpoint and focused backend tests. Existing team size limit remains 5 members including the leader.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Workflow Integrity**: PASS. Affected roles are TeamLeader and candidate Member/Student. State transition is creation of one `TeamMember` relationship for the current TeamLeader's own team. Failure states include invalid/empty StudentCode, user not found, unapproved/ineligible user, leader self-add, duplicate same-team member, same-event team conflict, full team, missing led team, unauthorized role, and locked team status. Scoring/ranking is unaffected because this changes team membership only.
- **Security and Role Boundaries**: PASS. New endpoint is restricted to authenticated `TeamLeader` only. Backend derives both actor and target team from the JWT identity and database state. Frontend does not collect or expose UserId/TeamId/GUID as add-member input. Error responses remain user-displayable and avoid exposing internal identifiers.
- **API Contract Discipline**: PASS. Add `POST /api/teams/my-team/members` with DTO `{ studentCode: string }`. Response returns updated team/member data, not just a message. Existing `POST /api/teams/{teamId}/members` may remain for compatibility or be refactored internally, but the frontend must consume the new my-team endpoint through `teamService` and `useAddTeamMember`.
- **Testable Business Behavior**: PASS. Add controller/API-level tests for success, role/ownership behavior via current-user led team resolution, not-found StudentCode, duplicate same team, same-event conflict, full team, unapproved user, and no membership mutation on failure. Existing relevant backend tests must continue to pass.
- **Observability and Auditability**: PASS. Successful additions and integrity-denied attempts must write audit records containing actor, target team when known, submitted StudentCode, outcome category, and timestamp through the existing audit log mechanism. Unexpected exceptions continue through centralized exception middleware.
- **Architecture Standards**: PASS. Uses existing ASP.NET Core controller/DTO/EF/Identity patterns and existing Next.js service/hook/page structure. No new dependency or architectural layer is needed.

## Project Structure

### Documentation (this feature)

```text
specs/001-add-team-member/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- add-team-member-api.md
|-- checklists/
|   `-- requirements.md
`-- spec.md
```

### Source Code (repository root)

```text
Backend/
|-- Controllers/
|   `-- TeamsController.cs
|-- DTOs/
|   `-- Team/
|       `-- AddTeamMenberRequest.cs
|-- Data/
|   `-- ApplicationDbContext.cs
|-- Models/
|   `-- Entities/
|       |-- Team.cs
|       |-- TeamMember.cs
|       `-- ApplicationUser.cs
`-- Tests/
    `-- SEAL.NET.Tests/
        `-- Controllers/
            `-- TeamsControllerAddMemberTests.cs

Frontend/
|-- app/
|   `-- (dashboard)/
|       `-- my-team/
|           `-- page.tsx
|-- components/
|   `-- team/
|       |-- TeamAddMemberPanel.tsx
|       `-- TeamInvitePanel.tsx
|-- hooks/
|   `-- useTeams.ts
|-- services/
|   `-- teamService.ts
`-- types/
    `-- team.ts
```

**Structure Decision**: Use the existing SEAL.NET split web application structure. Backend changes stay in the existing `TeamsController` and team DTO/test areas unless implementation reveals a local helper extraction is necessary to remove duplication. Frontend changes stay in the My Team page, a focused team component, typed service methods, hooks, and team types.

## Phase 0: Research

Research output is captured in [research.md](./research.md). Decisions:

- Add a new GUID-free endpoint instead of making frontend call the existing `{teamId}` endpoint.
- Keep `StudentCode` as the only request body field.
- Return an updated team representation so React Query can refresh or set member state deterministically.
- Reuse existing team validation order while adding audit events for success and denied membership-integrity attempts.
- Keep direct-add behavior separate from invite behavior on the My Team page.

## Phase 1: Design And Contracts

Design outputs:

- [data-model.md](./data-model.md)
- [contracts/add-team-member-api.md](./contracts/add-team-member-api.md)
- [quickstart.md](./quickstart.md)

Key implementation decisions:

- New API: `POST /api/teams/my-team/members`
- Authorization: `[Authorize(Roles = "TeamLeader")]`
- Team resolution: query current user's led team server-side; do not accept team id from request body or user-facing form.
- Frontend UI: replace or conditionally supersede `TeamInvitePanel` with a direct `TeamAddMemberPanel` that has one StudentCode input and uses `useAddTeamMember`.
- Refresh behavior: invalidate `TEAM_KEYS.myTeam` after success; optionally use returned team data to update cache immediately.

## Post-Design Constitution Check

- **Domain Workflow Integrity**: PASS. Contracts and data model preserve team membership rules and define failure behavior before implementation.
- **Security and Role Boundaries**: PASS. New endpoint removes frontend team GUID dependency and restricts the action to TeamLeader.
- **API Contract Discipline**: PASS. Contract documents route, request, success response, and structured error outcomes.
- **Testable Business Behavior**: PASS. Quickstart and plan identify backend test coverage for all specified rules and frontend verification through lint/build.
- **Observability and Auditability**: PASS. Audit requirements are explicit for success and denied integrity cases.
- **Architecture Standards**: PASS. No new architecture or dependencies introduced.

## Complexity Tracking

No constitutional violations or additional architectural complexity are required.
