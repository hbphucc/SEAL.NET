# Data Model: TeamLeader Add Team Member

## TeamLeader

Represents the authenticated actor allowed to manage one led team in this workflow.

**Source entity**: `ApplicationUser` plus Identity role membership.

**Relevant fields**:

- `Id`: internal GUID used only server-side.
- `StudentCode`: displayed where already part of user/member profiles; not used as actor lookup for this operation.
- `IsApproved`: must be true for normal account participation.
- Identity role: must include `TeamLeader`.

**Validation rules**:

- Request must be authenticated.
- Request must satisfy `TeamLeader` role.
- Actor must lead the target team resolved by `Team.LeaderId`.

## Team

Represents the participant group being modified.

**Source entity**: `Team`.

**Relevant fields**:

- `TeamId`: internal GUID, resolved server-side only for this workflow.
- `TeamName`
- `LeaderId`
- `Status`
- `CategoryId`
- `Members`
- `Category.EventId`

**Validation rules**:

- The team must exist.
- The team must be led by the authenticated TeamLeader.
- The team must not exceed the maximum member count of 5 including the leader.
- If existing behavior is preserved, only pending teams can be directly modified through this endpoint.

## Student/User

Represents the candidate member identified by the submitted StudentCode.

**Source entity**: `ApplicationUser`.

**Relevant fields**:

- `Id`: internal GUID used only server-side to create `TeamMember`.
- `StudentCode`: unique lookup identifier submitted by the TeamLeader.
- `FullName`
- `Email`
- `IsApproved`
- `StudentType`

**Validation rules**:

- `StudentCode` input is required and must be trimmed before lookup.
- Lookup should be case-insensitive if current StudentCode policy allows it.
- User must exist.
- User must be approved/eligible.
- User must not be the TeamLeader adding themself.

## Team Membership

Represents a user's membership in a team.

**Source entity**: `TeamMember`.

**Relevant fields**:

- `TeamMemberId`
- `TeamId`
- `UserId`
- `Role`
- `IsLeader`
- `JoinedAt`

**Validation rules**:

- A user cannot be added twice to the same team.
- A user cannot already belong to another team in the same event.
- New member is created with `Role = Member` and `IsLeader = false`.
- Existing database unique index on `(TeamId, UserId)` remains a final duplicate guard.

## Event

Represents the competition scope used to detect same-event team conflicts.

**Source entity**: `Event` through `Team.Category.EventId`.

**Relevant fields**:

- `EventId`
- `Categories`
- lifecycle fields such as registration/judging dates if used by existing team-edit rules.

**Validation rules**:

- Same-event conflict is evaluated by finding teams in categories that share the current team's `EventId`.

## Audit Log

Represents traceability for successful and denied add-member attempts.

**Source entity**: `AuditLog`.

**Relevant fields**:

- `Action`
- `EntityType`
- `EntityId`
- `ActorUserId`
- `Details`
- timestamp field if present in current entity definition.

**Validation rules**:

- Successful additions should record the actor, target team, submitted StudentCode, and success outcome.
- Denied authorization or membership-integrity attempts should record actor, submitted StudentCode, target team if known, and result category.
