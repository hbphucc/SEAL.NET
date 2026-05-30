# Quickstart: TeamLeader Add Team Member

## Preconditions

- Backend and frontend dependencies are installed.
- Database contains:
  - one approved user with `TeamLeader` role who leads a pending team;
  - one approved student with a unique `StudentCode` who is not in any team for the same event;
  - optional fixture users for duplicate, same-event conflict, unapproved, and team-full scenarios.

## Backend Verification

From `Backend/`:

```powershell
dotnet test Tests/SEAL.NET.Tests/SEAL.NET.Tests.csproj
```

Required new test coverage:

- TeamLeader can add an eligible student by StudentCode through `POST /api/teams/my-team/members`.
- Endpoint rejects non-TeamLeader users.
- Endpoint resolves the team from current identity and does not require route/body team GUID.
- Missing StudentCode fails without creating a membership.
- Unknown StudentCode fails without creating a membership.
- Unapproved/ineligible user fails without creating a membership.
- User already in the same team fails without duplicate membership.
- User already in another team in the same event fails.
- Full team fails.
- Successful and denied integrity attempts create audit records.

## API Smoke Test

```http
POST /api/teams/my-team/members
Authorization: Bearer <team-leader-token>
Content-Type: application/json

{
  "studentCode": "SE123456"
}
```

Expected:

- `200 OK`
- response includes `message`
- response includes either `team.members` containing the new member or `addedMember`
- no team id, user id, or GUID was submitted by the user-facing workflow

## Frontend Verification

From `Frontend/`:

```powershell
npm.cmd run lint
npm.cmd run build
```

Manual UX check:

1. Sign in as a TeamLeader.
2. Open My Team.
3. Confirm the add-member control has exactly one StudentCode input.
4. Submit an eligible StudentCode.
5. Confirm the new member appears in the visible member list without a manual page refresh.
6. Submit invalid cases and confirm the existing list is preserved while clear errors are shown.

## Implementation Notes

- The My Team page should call `useAddTeamMember`, not invite APIs, for this feature.
- The add-member service should call `POST /api/teams/my-team/members`.
- React Query should invalidate or update `TEAM_KEYS.myTeam` on success.
- The invite panel may remain for separate invite workflows, but it should not be the UI for this direct StudentCode-only feature.
