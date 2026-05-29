# Research: TeamLeader Add Team Member

## Decision: Add a GUID-free my-team endpoint

Use `POST /api/teams/my-team/members` for the frontend add-member workflow.

**Rationale**: The existing `POST /api/teams/{teamId}/members` endpoint already implements much of the needed validation, but it requires a team GUID in the route. The feature requires the user-facing workflow to avoid internal identifiers. A my-team endpoint lets the backend derive the target team from the authenticated TeamLeader and ensures the frontend sends only `StudentCode`.

**Alternatives considered**:

- Keep using `POST /api/teams/{teamId}/members`: rejected because the frontend would still need to pass a team GUID.
- Put team name/category in the request: rejected because it is less stable than resolving the TeamLeader's team server-side and can create ambiguity.

## Decision: Restrict direct add to TeamLeader role

Apply `[Authorize(Roles = "TeamLeader")]` to the new endpoint.

**Rationale**: The specification grants no new add-member permission to Member, Admin, Judge, or Mentor in this workflow. Server-side role enforcement must be authoritative, with the frontend only shaping the user experience.

**Alternatives considered**:

- Allow `Member,TeamLeader` and rely on `LeaderId` checks: rejected for the new endpoint because it weakens the explicit role boundary required by the feature.

## Decision: Resolve the led team from current identity

The backend should use the current authenticated user's id to find a team where `Team.LeaderId == currentUserId`, include members, users, category, and event context needed for validation, and reject the request if no eligible led team exists.

**Rationale**: This enforces ownership without trusting route/body input and keeps frontend forms free of UserId/TeamId/GUID input.

**Alternatives considered**:

- Let frontend select a team: rejected because TeamLeader add-member scope is only "my team" for this feature.
- Infer from current membership instead of leadership: rejected because members cannot manage rosters unless they are the leader.

## Decision: Preserve existing validation behavior and make outcomes distinct

Validation should cover empty StudentCode, missing led team, team locked/not pending if existing behavior remains, full team, StudentCode not found, unapproved/ineligible user, leader adding self, duplicate same-team membership, and same-event team membership conflict.

**Rationale**: The audit found these checks already exist in the current endpoint. Reusing the behavior minimizes risk while the new route fixes the frontend/internal-identifier problem.

**Alternatives considered**:

- Move all validation to frontend: rejected because backend authorization and membership integrity must be authoritative.

## Decision: Return updated member/team data on success

Return at least the added member representation, and preferably the updated team representation compatible with `Team` used by `getMyTeam`.

**Rationale**: The frontend must update the visible member list after a successful add. Returning updated data allows React Query to update cache immediately or refetch with confidence.

**Alternatives considered**:

- Return only `{ message }`: rejected because the audit identifies this as a gap and it forces the frontend to depend solely on an extra fetch.

## Decision: Use direct add UI, not invite UI

The My Team page should use a StudentCode-only add-member component for this feature. The existing invite panel can remain elsewhere or be hidden where direct add is required.

**Rationale**: The current `TeamInvitePanel` allows Email or StudentCode and creates invites, not direct memberships. The feature requires direct addition by StudentCode only.

**Alternatives considered**:

- Modify `TeamInvitePanel` to direct-add: acceptable only if renamed/repurposed clearly; otherwise rejected because it would blur invite and direct-add flows.

## Decision: Add focused backend tests

Add tests in `Backend/Tests/SEAL.NET.Tests` for the new add-member behavior.

**Rationale**: Team membership rules are role-sensitive and stateful; the constitution requires testable business behavior. Existing tests do not cover `TeamsController.AddMember`.

**Alternatives considered**:

- Manual-only testing: rejected because it would not protect authorization and persistence regressions.
