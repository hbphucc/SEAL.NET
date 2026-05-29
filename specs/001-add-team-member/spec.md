# Feature Specification: TeamLeader Add Team Member

**Feature Branch**: `001-add-team-member`

**Created**: 2026-05-30

**Status**: Draft

**Input**: User description: "Tôi muốn xây dựng chức năng TeamLeader thêm thành viên vào team bằng StudentCode. Người dùng là TeamLeader. TeamLeader chỉ được thêm thành viên vào team của chính mình. Input chỉ là StudentCode, không dùng UserId/GUID ở frontend. Nếu StudentCode không tồn tại thì trả lỗi rõ ràng. Nếu user đã ở trong team thì không được thêm lại. Nếu user đã thuộc team khác trong cùng event thì không được thêm. Nếu team đã đủ số lượng thành viên thì không được thêm. Sau khi thêm thành công, frontend phải cập nhật danh sách thành viên. Backend dùng ASP.NET Core Web API, EF Core, Identity. Frontend dùng Next.js TypeScript. Chỉ tạo specification trước, không sửa code."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Add Member By StudentCode (Priority: P1)

As a TeamLeader, I can add a student to my own team by entering only that student's StudentCode, so that I can manage team membership without exposing internal user identifiers.

**Why this priority**: This is the core workflow and delivers the primary value of the feature.

**Independent Test**: Can be fully tested by signing in as a TeamLeader, entering a valid StudentCode for an eligible student, submitting the add action, and confirming the member appears in the team member list.

**Acceptance Scenarios**:

1. **Given** a signed-in TeamLeader viewing their own team and an eligible student who is not already assigned to a team in the same event, **When** the TeamLeader submits that student's StudentCode, **Then** the student is added to the TeamLeader's team and the updated member list is shown.
2. **Given** a signed-in TeamLeader viewing their own team, **When** the add-member form is displayed, **Then** the only student identifier accepted from the TeamLeader is StudentCode and no internal user identifier is required or shown as an input.

---

### User Story 2 - Prevent Invalid Member Additions (Priority: P1)

As a TeamLeader, I receive clear feedback when a student cannot be added, so that I understand what must be corrected without creating invalid team membership.

**Why this priority**: The feature must protect event and team membership rules as strongly as it enables successful additions.

**Independent Test**: Can be tested by attempting to add StudentCodes that do not exist, already belong to the current team, belong to another team in the same event, or exceed the team's member limit.

**Acceptance Scenarios**:

1. **Given** a TeamLeader submits a StudentCode that does not match any user, **When** the add action is processed, **Then** the student is not added and a clear "StudentCode not found" style error is shown.
2. **Given** a TeamLeader submits a StudentCode for a user already in the same team, **When** the add action is processed, **Then** the user is not added again and a clear duplicate-member error is shown.
3. **Given** a TeamLeader submits a StudentCode for a user already assigned to another team in the same event, **When** the add action is processed, **Then** the user is not added and a clear same-event team conflict error is shown.
4. **Given** the TeamLeader's team has reached its member limit, **When** the TeamLeader submits any otherwise valid StudentCode, **Then** no member is added and a clear team-full error is shown.

---

### User Story 3 - Enforce Own-Team Authorization (Priority: P1)

As a TeamLeader, I can only add members to the team that I lead, so that no TeamLeader can modify another team's roster.

**Why this priority**: Authorization is a critical integrity rule for team membership and event fairness.

**Independent Test**: Can be tested by signing in as one TeamLeader and attempting to add a member to another team's roster; the action must be denied and no membership must change.

**Acceptance Scenarios**:

1. **Given** a signed-in TeamLeader attempts to add a member to a team they do not lead, **When** the add action is processed, **Then** the action is denied and the target team's member list remains unchanged.
2. **Given** a signed-in TeamLeader has no active team leadership for the relevant team, **When** they attempt to add a member, **Then** the action is denied with a clear authorization error.

### Edge Cases

- StudentCode is empty, whitespace-only, or in an invalid format: the member is not added and the TeamLeader sees a clear validation error.
- StudentCode casing or surrounding whitespace differs from the stored value: the system normalizes harmless input differences before matching where StudentCode policy allows it.
- Multiple add attempts for the same StudentCode are submitted close together: only one membership can be created.
- The team's remaining capacity changes before submission is processed: the latest capacity is enforced.
- The target user exists but is not eligible to participate as a student/member: the user is not added and a clear eligibility error is shown.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow a signed-in TeamLeader to submit a StudentCode to add a member to the team they lead.
- **FR-002**: The user-facing add-member input MUST accept StudentCode only and MUST NOT require or expose internal user identifiers such as UserId or GUID.
- **FR-003**: The system MUST verify on the server side that the requesting TeamLeader leads the target team before adding any member.
- **FR-004**: The system MUST reject attempts by a TeamLeader to add members to a team they do not lead.
- **FR-005**: The system MUST reject a submitted StudentCode that does not match an existing user and return a clear, actionable error.
- **FR-006**: The system MUST reject attempts to add a user who is already a member of the same team.
- **FR-007**: The system MUST reject attempts to add a user who already belongs to another team in the same event.
- **FR-008**: The system MUST reject attempts to add a member when the team has reached its allowed member limit.
- **FR-009**: The system MUST reject empty, malformed, or otherwise invalid StudentCode input before any membership change is made.
- **FR-010**: The system MUST update the visible team member list after a successful add so the TeamLeader can immediately see the new member.
- **FR-011**: The system MUST preserve the current member list when an add attempt fails.
- **FR-012**: The system MUST return distinct error outcomes for not found, duplicate in same team, assigned to another same-event team, team full, invalid input, ineligible user, and unauthorized team access.
- **FR-013**: The system MUST prevent duplicate membership creation when repeated or concurrent add requests target the same StudentCode and team.
- **FR-014**: The system MUST record an audit/logging event for successful member additions and denied attempts that affect authorization or membership integrity.

### Key Entities *(include if feature involves data)*

- **TeamLeader**: A signed-in user with responsibility for exactly the team they are allowed to manage in this workflow.
- **Team**: A participant group within an event, with a leader, current members, and an allowed member limit.
- **Student/User**: A person identified to TeamLeaders by StudentCode and eligible to become a team member when event rules allow it.
- **Team Membership**: The relationship between a user and a team, scoped by the team's event for conflict checks.
- **Event**: The competition or activity context that determines whether a user already belongs to another team in the same event.

### Security, Audit & Contracts *(mandatory for SEAL.NET features)*

- **Roles impacted**: TeamLeader can add members to their own team only. Other roles are not granted new member-add permissions by this feature.
- **Authorization rules**: Every add attempt must validate the signed-in user's TeamLeader role and ownership of the target team before membership checks complete. Unauthorized attempts must be denied without changing any team membership.
- **API contracts**: The add-member contract accepts a target team context and a request containing StudentCode. It returns the updated team member representation on success and structured, user-displayable errors for validation, authorization, not-found, duplicate, same-event team conflict, ineligible user, and team-full outcomes.
- **Audit/logging**: Record who attempted the action, target team, submitted StudentCode, result category, and timestamp for successful additions and denied authorization or membership-integrity attempts.
- **Sensitive data**: StudentCode is personally identifying academic data and must be handled only for lookup and display needed by the team-management workflow. Internal user identifiers must not be collected from or exposed as inputs to TeamLeaders.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 95% of valid member additions can be completed by a TeamLeader in under 30 seconds from opening the add-member control.
- **SC-002**: 100% of invalid add attempts covered by the specified business rules result in no membership change and a clear error message.
- **SC-003**: 100% of successful additions show the newly added member in the visible member list without requiring a manual page refresh.
- **SC-004**: 0 internal user identifiers are required from TeamLeaders during the add-member workflow.
- **SC-005**: 100% of attempts to modify a team not led by the signed-in TeamLeader are denied and leave the target roster unchanged.

## Assumptions

- A TeamLeader already has an authenticated session and an existing team context before using this feature.
- Each team belongs to one event, and the same-event conflict rule is evaluated through that event relationship.
- The team member limit already exists as part of event or team rules and is authoritative for this feature.
- StudentCode uniquely identifies a user for member lookup within the system.
- This specification covers adding members only; removing members, changing leaders, creating teams, and editing event capacity rules are out of scope.
- Existing member list display already exists or will be available to refresh after a successful add.
