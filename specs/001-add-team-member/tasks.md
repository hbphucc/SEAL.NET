# Tasks: TeamLeader Add Team Member

**Input**: Design documents from `specs/001-add-team-member/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/add-team-member-api.md](./contracts/add-team-member-api.md), [quickstart.md](./quickstart.md)

**Tests**: Required. This feature changes authorization, persistence, API contract behavior, and role-sensitive team membership rules.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files or does not depend on incomplete tasks
- **[Story]**: User story label, used only for user story phases
- Every task includes an exact file path

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm current implementation surface and prepare the task execution area.

- [ ] T001 Review the existing direct add-member endpoint and invite endpoint in `Backend/Controllers/TeamsController.cs`
- [ ] T002 [P] Review existing frontend team service, hook, and My Team page usage in `Frontend/services/teamService.ts`, `Frontend/hooks/useTeams.ts`, and `Frontend/app/(dashboard)/my-team/page.tsx`
- [ ] T003 [P] Review existing controller test setup patterns in `Backend/Tests/SEAL.NET.Tests/Controllers/AdminTeamsControllerTests.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared helpers and contract types that all stories depend on.

**CRITICAL**: No user story implementation should begin until this phase is complete.

- [ ] T004 Create `TeamsControllerAddMemberTests` test fixture helpers for EF InMemory context, Identity `UserManager`, authenticated controller user claims, categories, teams, and users in `Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs`
- [ ] T005 Add or confirm a reusable Team response DTO/shape for updated team member responses in `Backend/DTOs/Team/AddTeamMenberRequest.cs`
- [ ] T006 Add a private response-mapping helper for My Team member output in `Backend/Controllers/TeamsController.cs`
- [ ] T007 Update frontend add-member response types for the new my-team contract in `Frontend/types/team.ts`

**Checkpoint**: Shared test and contract scaffolding is ready.

---

## Phase 3: User Story 1 - Add Member By StudentCode (Priority: P1) MVP

**Goal**: TeamLeader can add an eligible student to their own team using only StudentCode, and the visible member list updates after success.

**Independent Test**: Sign in as a TeamLeader with a pending team, submit an eligible StudentCode from My Team, and confirm the new member appears without manually refreshing.

### Tests for User Story 1

> Write these tests first and confirm they fail before implementation.

- [ ] T008 [P] [US1] Add backend success test for `POST /api/teams/my-team/members` adding an eligible StudentCode and persisting one `TeamMember` in `Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs`
- [ ] T009 [P] [US1] Add backend success response test asserting the response includes the added member or updated team member list in `Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs`

### Implementation for User Story 1

- [X] T010 [US1] Implement `POST /api/teams/my-team/members` success path that resolves the current TeamLeader's led team server-side in `Backend/Controllers/TeamsController.cs`
- [X] T011 [US1] Return the updated team representation and added member data from the successful add-member response in `Backend/Controllers/TeamsController.cs`
- [X] T012 [US1] Add a successful add-member audit log entry with actor id, team id, StudentCode, and outcome details in `Backend/Controllers/TeamsController.cs`
- [X] T013 [US1] Change `teamService.addMember` to call `POST /teams/my-team/members` without a teamId argument in `Frontend/services/teamService.ts`
- [X] T014 [US1] Update `useAddTeamMember` to accept only `{ studentCode }`, invalidate `TEAM_KEYS.myTeam`, and optionally set returned team data in `Frontend/hooks/useTeams.ts`
- [X] T015 [P] [US1] Create a StudentCode-only direct add-member form component in `Frontend/components/team/TeamAddMemberPanel.tsx`
- [X] T016 [US1] Replace the My Team direct member-add UI from invite flow to `TeamAddMemberPanel` in `Frontend/app/(dashboard)/my-team/page.tsx`
- [X] T017 [US1] Ensure the My Team add-member form exposes no teamId, userId, or GUID input and only submits StudentCode in `Frontend/app/(dashboard)/my-team/page.tsx`
- [X] T018 [US1] Verify the Story 1 API and UI behavior against the smoke-test steps in `specs/001-add-team-member/quickstart.md`

**Checkpoint**: User Story 1 is functional and independently testable.

---

## Phase 4: User Story 2 - Prevent Invalid Member Additions (Priority: P1)

**Goal**: Invalid add attempts show clear errors and leave team membership unchanged.

**Independent Test**: Submit StudentCodes for not found, duplicate same team, same-event conflict, unapproved user, and team-full cases; confirm each fails without changing the member list.

### Tests for User Story 2

> Write these tests first and confirm they fail before implementation.

- [X] T019 [P] [US2] Add backend tests for empty and unknown StudentCode returning clear errors with no membership mutation in `Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs`
- [X] T020 [P] [US2] Add backend tests for duplicate same-team member and leader self-add returning clear errors with no duplicate membership in `Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs`
- [X] T021 [P] [US2] Add backend tests for same-event team conflict and team-full rejection in `Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs`
- [X] T022 [P] [US2] Add backend tests for unapproved or ineligible candidate user rejection in `Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs`
- [X] T023 [P] [US2] Add backend audit tests for denied membership-integrity outcomes in `Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs`

### Implementation for User Story 2

- [X] T024 [US2] Reuse or extract validation logic so the my-team add-member endpoint rejects empty, unknown, unapproved, self-add, duplicate, same-event conflict, full-team, and locked-team cases in `Backend/Controllers/TeamsController.cs`
- [X] T025 [US2] Ensure each invalid add-member outcome returns a distinct user-displayable `{ message }` response in `Backend/Controllers/TeamsController.cs`
- [X] T026 [US2] Add audit log records for denied duplicate, same-event conflict, team-full, unapproved, and missing-led-team attempts in `Backend/Controllers/TeamsController.cs`
- [X] T027 [US2] Preserve the existing member list and display backend error messages in the direct add-member form in `Frontend/components/team/TeamAddMemberPanel.tsx`

**Checkpoint**: User Story 2 invalid cases are independently testable and do not mutate membership.

---

## Phase 5: User Story 3 - Enforce Own-Team Authorization (Priority: P1)

**Goal**: TeamLeader can only add members to the team they lead, with no frontend-provided target team identifier.

**Independent Test**: Authenticate as a TeamLeader with no led team or a user without TeamLeader role and confirm the endpoint is denied and no other team's roster changes.

### Tests for User Story 3

> Write these tests first and confirm they fail before implementation.

- [X] T028 [P] [US3] Add backend test asserting the my-team add-member action is decorated for `TeamLeader` role authorization in `Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs`
- [X] T029 [P] [US3] Add backend test for a TeamLeader with no led team returning a clear denial/not-found response with no membership mutation in `Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs`
- [X] T030 [P] [US3] Add backend test proving the endpoint resolves the target team from current user identity and cannot add to another TeamLeader's team in `Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs`

### Implementation for User Story 3

- [X] T031 [US3] Restrict the new my-team add-member endpoint to `[Authorize(Roles = "TeamLeader")]` in `Backend/Controllers/TeamsController.cs`
- [X] T032 [US3] Remove target team id from the frontend add-member hook/service call path while preserving existing non-input rendering needs in `Frontend/hooks/useTeams.ts` and `Frontend/services/teamService.ts`
- [X] T033 [US3] Render `TeamAddMemberPanel` only for the current leader and never pass a user-facing teamId/GUID add-member input in `Frontend/app/(dashboard)/my-team/page.tsx`

**Checkpoint**: User Story 3 authorization boundary is independently testable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, cleanup, and documentation consistency.

- [X] T034 [P] Update contract examples if implementation response fields differ from the planned shape in `specs/001-add-team-member/contracts/add-team-member-api.md`
- [X] T035 [P] Update quickstart verification notes with any exact command or response adjustments in `specs/001-add-team-member/quickstart.md`
- [X] T036 Run backend test suite and record any failures for this feature in `Backend/Tests/SEAL.NET.Tests/SEAL.NET.Tests.csproj`
- [X] T037 Run frontend lint and build checks for the My Team changes in `Frontend/package.json`
- [X] T038 Review the final implementation against the no UserId/GUID input rule in `Frontend/app/(dashboard)/my-team/page.tsx` and `Frontend/components/team/TeamAddMemberPanel.tsx`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **User Stories (Phases 3-5)**: Depend on Foundational completion.
- **Polish (Phase 6)**: Depends on completed target user stories.

### User Story Dependencies

- **US1 (P1)**: MVP. Can start after Foundational and delivers the successful direct add-member workflow.
- **US2 (P1)**: Can start after Foundational, but implementation is simplest after US1 creates the endpoint path.
- **US3 (P1)**: Can start after Foundational, but final verification should run after US1 endpoint exists.

### Within Each User Story

- Tests first, then implementation.
- Backend contract and validation before frontend integration.
- Frontend service/hook before page/component wiring.
- Story checkpoint before moving to polish.

---

## Parallel Opportunities

- T002 and T003 can run in parallel with T001.
- T005, T006, and T007 can be split after T004 defines shared test needs.
- US1 tests T008 and T009 can be written in parallel.
- T015 can be developed in parallel with backend T010-T012 after T007 defines frontend response types.
- US2 tests T019-T023 can be written in parallel because they cover separate failure scenarios in the same planned test file.
- US3 tests T028-T030 can be written in parallel because they cover separate authorization scenarios.
- Polish documentation T034 and T035 can run in parallel.

## Parallel Example: User Story 1

```text
Task: "T008 Add backend success test for POST /api/teams/my-team/members in Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs"
Task: "T009 Add backend success response test in Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs"
Task: "T015 Create StudentCode-only direct add-member form in Frontend/components/team/TeamAddMemberPanel.tsx"
```

## Parallel Example: User Story 2

```text
Task: "T019 Add backend tests for empty and unknown StudentCode in Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs"
Task: "T020 Add backend tests for duplicate same-team member and leader self-add in Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs"
Task: "T021 Add backend tests for same-event conflict and team-full rejection in Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs"
Task: "T022 Add backend tests for unapproved or ineligible candidate user in Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "T028 Add authorization attribute test in Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs"
Task: "T029 Add no-led-team denial test in Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs"
Task: "T030 Add current-user team resolution test in Backend/Tests/SEAL.NET.Tests/Controllers/TeamsControllerAddMemberTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete US1 tests T008-T009.
3. Implement US1 tasks T010-T018.
4. Validate direct add-member success flow from My Team.

### Incremental Delivery

1. Deliver US1 direct StudentCode add-member workflow.
2. Deliver US2 invalid-case protections and audit coverage.
3. Deliver US3 authorization hardening and no-GUID user-facing flow verification.
4. Complete polish validation tasks.

### Task Count Summary

- Total tasks: 38
- Setup: 3
- Foundational: 4
- US1: 11
- US2: 9
- US3: 6
- Polish: 5

## Notes

- Existing `POST /api/teams/{teamId}/members` may remain for compatibility, but the My Team frontend must use `POST /api/teams/my-team/members`.
- Existing invite behavior may remain for separate workflows, but it must not be the UI used for this direct add-member feature.
- All task checklist lines follow `- [ ] T### [P?] [US?] Description with file path`.
