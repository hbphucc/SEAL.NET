# API Contract: Add Member To My Team

## POST /api/teams/my-team/members

Adds a student directly to the authenticated TeamLeader's own team using StudentCode only.

### Authorization

- Requires authenticated JWT session.
- Requires `TeamLeader` role.
- The backend resolves the target team from the current user's identity.
- No team id, user id, or GUID is accepted from the request body.

### Request

```http
POST /api/teams/my-team/members
Content-Type: application/json
Authorization: Bearer <token>
```

```json
{
  "studentCode": "SE123456"
}
```

### Request Validation

- `studentCode` is required.
- Surrounding whitespace is trimmed before lookup.
- Empty/whitespace-only values return a validation error.
- Matching is case-insensitive if consistent with current StudentCode lookup policy.

### Success Response

Preferred response shape returns the updated team so the frontend can refresh the visible member list immediately.

```http
200 OK
```

```json
{
  "message": "Member added successfully.",
  "team": {
    "teamId": "server-internal-guid",
    "teamName": "Seal Builders",
    "status": "Pending",
    "leaderId": "server-internal-guid",
    "category": {
      "categoryId": "server-internal-guid",
      "categoryName": "Software"
    },
    "currentRound": null,
    "members": [
      {
        "userId": "server-internal-guid",
        "studentCode": "SE111111",
        "fullName": "Team Leader",
        "email": "leader@example.com",
        "role": "Leader",
        "isLeader": true
      },
      {
        "userId": "server-internal-guid",
        "studentCode": "SE123456",
        "fullName": "New Member",
        "email": "member@example.com",
        "role": "Member",
        "isLeader": false
      }
    ]
  },
  "addedMember": {
    "studentCode": "SE123456",
    "fullName": "New Member",
    "email": "member@example.com",
    "role": "Member",
    "isLeader": false
  }
}
```

Frontend must not display or request GUID fields as part of the add-member form. Existing team/member types may still contain internal ids for already-existing application rendering and React keys.

### Error Responses

All error responses should include a user-displayable `message`.

```json
{
  "message": "Student code is required."
}
```

Expected outcomes:

| Status | Condition | Message guidance |
|--------|-----------|------------------|
| 400 | Empty/invalid StudentCode | `Student code is required.` or equivalent validation text |
| 401 | Not authenticated | Existing auth behavior |
| 403 | Authenticated but not TeamLeader | Existing authorization behavior |
| 404 | TeamLeader has no led team | `Team not found.` or `You do not lead a team.` |
| 404 | StudentCode not found | `User with Student Code '<code>' was not found.` |
| 400 | User not approved/ineligible | `This user has not been approved yet.` |
| 400 | Leader adds self | `Leader is already part of the team.` |
| 400 | User already in same team | `User is already in this team.` |
| 400 | User in another team in same event | `User already joined another team in this event.` |
| 400 | Team full | `A team can have maximum 5 members.` |
| 400 | Team membership locked by status/lifecycle | Existing locked-membership message |

### Audit Requirements

Create an audit record for:

- successful add-member operation;
- denied same-team duplicate;
- denied same-event conflict;
- denied team-full attempt;
- denied ineligible/unapproved user;
- denied missing/unauthorized led-team condition when actor is known.

Audit details should include actor id, target team id when known, submitted StudentCode, and outcome category.
