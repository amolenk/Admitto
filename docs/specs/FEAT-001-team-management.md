# Feature Specification: Team Management

## 1. Overview

| Field           | Value                                          |
| --------------- | ---------------------------------------------- |
| Feature ID      | FEAT-001                                       |
| Status          | Draft                                          |
| Author          | Copilot + User                                 |
| Created         | 2026-03-26                                     |
| Last updated    | 2026-03-26                                     |
| Epic / Parent   | Organization Module                            |
| Arc42 reference | 5. Building Block View — Organization module   |

### 1.1 Problem Statement

Organizers need a way to create and manage teams — the foundational organizational
unit in Admitto. Without team management, there is no way to group people, assign
roles, or scope events. Archiving is currently missing, so unused teams accumulate
with no way to retire them without deletion.

### 1.2 Goal

Admins can create teams. Team owners can update team details and archive teams they
no longer need. All authenticated users with the appropriate role can list and view
teams. Archived teams are excluded from normal workflows but remain queryable for
audit purposes.

### 1.3 Non-Goals

- Team member management (separate feature — membership/roles)
- Team deletion (archive is the soft-removal mechanism)
- Team-scoped event management (covered by ticketed events spec)
- Public/unauthenticated team discovery
- Unarchiving teams (archiving is a one-way operation)

## 2. User Stories

### US-001: Create a team

**As an** admin,
**I want** to create a new team with a unique slug, display name, and contact email,
**so that** organizers have a workspace to manage events.

### US-002: View team details

**As a** team member (Crew or above),
**I want** to view my team's details,
**so that** I can see the current team information.

### US-003: List all teams

**As an** admin,
**I want** to list all teams on the platform,
**so that** I can oversee and manage the organizational landscape.

### US-004: Update team details

**As a** team owner,
**I want** to update my team's slug, name, or email address,
**so that** team information stays accurate as things change.

### US-005: Archive a team

**As a** team owner,
**I want** to archive a team that is no longer active,
**so that** it is retired from normal workflows without losing historical data.

### US-006: List my teams

**As an** authenticated user,
**I want** to see a list of teams I belong to,
**so that** I can navigate to the teams I am involved with.

## 3. Functional Requirements

| ID     | Requirement                                                                                                        | Priority | User Story |
| ------ | ------------------------------------------------------------------------------------------------------------------ | -------- | ---------- |
| FR-001 | The system shall allow admins to create a team with a slug, name, and email address.                               | Must     | US-001     |
| FR-002 | The system shall enforce globally unique team slugs.                                                                | Must     | US-001     |
| FR-003 | The system shall validate that slug, name, and email conform to the domain value object constraints.               | Must     | US-001     |
| FR-004 | The system shall allow team members with Crew role or above to retrieve a team's details by slug.                  | Must     | US-002     |
| FR-005 | The system shall allow admins to list all active teams.                                                             | Must     | US-003     |
| FR-006 | The system shall exclude archived teams from listings by default.                                                   | Should   | US-003     |
| FR-007 | The system shall allow team owners to update a team's slug, name, and/or email address (partial update).           | Must     | US-004     |
| FR-008 | The system shall use optimistic concurrency (expected version) to prevent lost updates.                            | Must     | US-004     |
| FR-009 | The system shall allow team owners to archive an active team.                                                       | Must     | US-005     |
| FR-010 | The system shall prevent modifications to an archived team.                                                         | Must     | US-005     |
| FR-011 | The system shall allow authenticated users to list teams they are a member of.                                     | Must     | US-006     |
| FR-012 | The system shall not provide a mechanism to unarchive a team; archiving is a one-way operation.                    | Must     | US-005     |
| FR-013 | The system shall prevent archiving a team that has active (upcoming) ticketed events. This check must be atomic with the archive operation to prevent race conditions. | Must     | US-005     |
| FR-014 | The system shall prevent creating ticketed events for an archived team. Both archive and event-creation operations must serialize through the Team aggregate's concurrency token to prevent race conditions. | Must     | US-005     |

## 4. Acceptance Scenarios

### SC-001: Successfully create a team (FR-001, FR-003)

```gherkin
Given an authenticated admin
When they create a team with slug "acme", name "Acme Events", and email "info@acme.org"
Then the team is created with the provided details
  And the team is in an active state
```

### SC-002: Reject duplicate slug on create (FR-002)

```gherkin
Given a team with slug "acme" already exists
  And an authenticated admin
When they create a team with slug "acme"
Then the request is rejected with a duplicate slug error
  And no new team is created
```

### SC-003: Reject invalid input on create (FR-003)

```gherkin
Given an authenticated admin
When they create a team with an empty name
Then the request is rejected with a validation error indicating the name is required
```

### SC-004: View team details by slug (FR-004)

```gherkin
Given a team "acme" exists
  And the user is a member of team "acme" with Crew role
When they request the details of team "acme"
Then the team's slug, name, email address, and version are returned
```

### SC-005: Reject unauthorized team view (FR-004)

```gherkin
Given a team "acme" exists
  And the user is not a member of team "acme"
When they request the details of team "acme"
Then the request is rejected as unauthorized
```

### SC-006: Admin lists all active teams (FR-005, FR-006)

```gherkin
Given teams "acme" (active) and "beta" (active) and "retired" (archived) exist
  And the user is an admin
When they list all teams
Then "acme" and "beta" are returned
  And "retired" is not included
```

### SC-007: Update team details with partial fields (FR-007, FR-008)

```gherkin
Given a team "acme" exists at version 1
  And the user is an owner of team "acme"
When they update the team name to "Acme Corp" with expected version 1
Then the team name is changed to "Acme Corp"
  And the slug and email remain unchanged
  And the version is incremented
```

### SC-008: Concurrent update conflict (FR-008)

```gherkin
Given a team "acme" exists at version 2
  And the user is an owner of team "acme"
When they update the team with expected version 1
Then the request is rejected with a concurrency conflict error
  And the team is not modified
```

### SC-009: Successfully archive a team (FR-009)

```gherkin
Given a team "acme" exists and is active
  And team "acme" has no active ticketed events
  And the user is an owner of team "acme"
When they archive team "acme"
Then the team status is changed to archived
```

### SC-010: Reject update of archived team (FR-010)

```gherkin
Given a team "acme" exists and is archived
  And the user is an owner of team "acme"
When they attempt to update the team name
Then the request is rejected because the team is archived
```

### SC-011: Reject archiving an already archived team (FR-010, FR-012)

```gherkin
Given a team "acme" exists and is archived
  And the user is an owner of team "acme"
When they attempt to archive team "acme"
Then the request is rejected because the team is already archived
```

### SC-012: List my teams (FR-011)

```gherkin
Given the user is a member of teams "acme" and "beta"
  And team "gamma" exists but the user is not a member
When they list their teams
Then "acme" and "beta" are returned
  And "gamma" is not included
```

### SC-013: Archived teams excluded from my teams list (FR-011, FR-006)

```gherkin
Given the user is a member of teams "acme" (active) and "beta" (archived)
When they list their teams
Then only "acme" is returned
```

### SC-014: Reject archiving a team with active events (FR-013)

```gherkin
Given a team "acme" exists and is active
  And team "acme" has an upcoming ticketed event
  And the user is an owner of team "acme"
When they attempt to archive team "acme"
Then the request is rejected because the team has active events
  And the team remains active
```

### SC-015: Reject creating an event for an archived team (FR-014)

```gherkin
Given a team "acme" exists and is archived
When an organizer attempts to create a ticketed event for team "acme"
Then the request is rejected because the team is archived
  And no event is created
```

### SC-016: Concurrent archive and event creation are serialized (FR-013, FR-014)

```gherkin
Given a team "acme" exists and is active with no active events
When an owner archives team "acme" and an organizer simultaneously creates an event for team "acme"
Then exactly one operation succeeds and the other is rejected with a concurrency conflict
  And the system remains in a consistent state
```

## 5. Domain Model

### 5.1 Entities

#### Team (Aggregate Root)

_The core organizational unit. Groups members who collaborate on events._

| Attribute    | Type                | Constraints                           | Description                      |
| ------------ | ------------------- | ------------------------------------- | -------------------------------- |
| id           | TeamId (UUID)       | PK, generated at creation             | Unique identity                  |
| slug         | Slug (string)       | required, unique, URL-safe, max ~50   | Human-readable URL identifier    |
| name         | DisplayName (string)| required, max ~200 chars              | Display name of the team         |
| emailAddress | EmailAddress (string)| required, valid email, max ~320 chars | Contact email for the team       |
| isArchived   | boolean             | defaults to false                     | Whether the team is archived     |
| version      | uint                | auto-incremented on save              | Optimistic concurrency token     |

#### TeamMembership (included for context)

_Tracks which users belong to which teams and their role. CRUD lifecycle is out of
scope for this spec; included because FR-011 (my teams list) queries this data._

| Attribute | Type               | Constraints         | Description             |
| --------- | ------------------ | ------------------- | ----------------------- |
| id        | TeamId (UUID)      | PK, FK to Team      | The team                |
| role      | TeamMembershipRole | required, enum      | Crew / Organizer / Owner|

### 5.2 Relationships

- A **Team** has many **TeamMemberships** (one-to-many)
- A **TeamMembership** belongs to exactly one **Team**

### 5.3 Value Objects

_All defined in the shared kernel (`Admitto.Module.Shared.Kernel`)._

| Value Object        | Attributes | Constraints                    |
| ------------------- | ---------- | ------------------------------ |
| TeamId              | GUID       | Not empty                      |
| Slug                | string     | URL-safe format, max length    |
| DisplayName         | string     | Non-empty, max length          |
| EmailAddress        | string     | Valid email format, max length  |
| TeamMembershipRole  | enum       | Crew, Organizer, Owner         |

### 5.4 Domain Rules and Invariants

- **Unique slug**: No two teams may share the same slug (enforced at database level
  via unique index).
- **Archive is terminal**: Once a team is archived (`isArchived = true`), it cannot
  be modified or unarchived.
- **Archive blocks mutation**: Attempting to change slug, name, or email on an
  archived team must raise a domain error.
- **Active events block archive**: A team cannot be archived while it has ticketed
  events that have not ended or been cancelled.
- **Archive blocks event creation**: A ticketed event cannot be created for an
  archived team.
- **Serialized team mutations**: Both archiving a team and creating events for a
  team must go through the Team aggregate's optimistic concurrency token. This
  prevents a race condition where an event is created concurrently with an archive
  operation (or vice versa).
- **Optimistic concurrency**: Updates must supply the expected version; mismatches
  are rejected.

## 6. Non-Functional Requirements

_Project-wide NFRs (authentication enforcement, observability, error response format)
apply per arc42 Section 10 and are not repeated here._

| ID      | Category    | Requirement                                                               |
| ------- | ----------- | ------------------------------------------------------------------------- |
| NFR-001 | Security    | Only users with the admin role may create teams or list all teams.        |
| NFR-002 | Security    | Only team owners may update or archive their team.                        |
| NFR-003 | Security    | Only team members with Crew role or above may view team details.          |
| NFR-004 | Performance | Team list endpoints shall return within 500ms for up to 1,000 teams.     |

## 7. Edge Cases and Error Scenarios

| ID   | Scenario                                            | Expected Behavior                                             |
| ---- | --------------------------------------------------- | ------------------------------------------------------------- |
| EC-1 | Admin creates a team with a slug that already exists| Return validation error; no team created                      |
| EC-2 | Owner updates slug to one already taken              | Return validation error; team not modified                    |
| EC-3 | Owner updates team with a stale version number       | Return concurrency conflict error (409); team not modified    |
| EC-4 | Owner attempts to update an archived team            | Return domain error indicating team is archived               |
| EC-5 | Owner attempts to archive an already-archived team   | Return domain error indicating team is already archived       |
| EC-6 | Non-member requests team details                     | Return 403 Forbidden                                          |
| EC-7 | Request for a team slug that does not exist           | Return 404 Not Found                                          |
| EC-8 | User with no team memberships lists "my teams"       | Return empty list (not an error)                              |
| EC-9 | Team with active ticketed events is archived         | Return domain error; team remains active                      |
| EC-10| Event created for an archived team                    | Return domain error; no event created                         |
| EC-11| Concurrent archive + event creation for same team     | One succeeds, other gets concurrency conflict (409); system stays consistent |

## 8. Success Criteria

| ID     | Criterion                                                                         |
| ------ | --------------------------------------------------------------------------------- |
| SC-001 | All 16 acceptance scenarios pass in CI.                                           |
| SC-002 | Archived teams are excluded from all listing endpoints.                           |
| SC-003 | Optimistic concurrency prevents lost updates under concurrent access.             |
| SC-004 | Authorization rules are enforced per role (admin, owner, crew) on every endpoint. |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Existing Team aggregate and Organization module infrastructure (partially built).
- TeamMembership data (for "my teams" list — FR-011).
- TicketedEvent data in the Organization module (for archive guard — FR-013).
- Shared kernel value objects (TeamId, Slug, DisplayName, EmailAddress).

### 9.2 Constraints

- Archiving is a one-way, irreversible operation.
- Cross-module queries (if needed for the active-events guard) must go through the
  Organization module's own write store or facade — not cross-module DbContext access.

### 9.3 Architecture References

| Arc42 Section                    | Relevance to This Feature                                         |
| -------------------------------- | ----------------------------------------------------------------- |
| 3. Context & Scope               | Admin/team API boundary; external event sites are not in scope    |
| 5. Building Block View           | Organization module owns team lifecycle                           |
| 6. Runtime View                  | Endpoint → handler → write store transaction flow                 |
| 8. Crosscutting Concepts         | Validation (FluentValidation), auth (JWT + role policies), unit of work (endpoint-owned), error handling (BusinessRuleViolationException) |
| 9. Architecture Decisions (ADRs) | ADR-001 (modular monolith), ADR-002 (feature-sliced endpoints)    |
| 10. Quality Requirements         | Maintainability, reliability, security quality goals              |

## 10. Open Questions

_No open questions. All decisions resolved during specification._

| #   | Question | Owner | Status   | Resolution                              |
| --- | -------- | ----- | -------- | --------------------------------------- |
| 1   | Should archiving be reversible? | User | Resolved | No — archiving is one-way |
| 2   | Should listing be admin-only or include "my teams"? | User | Resolved | Both: admin list + "my teams" |
| 3   | Can a team with active events be archived? | User | Resolved | No — must end/cancel events first |

