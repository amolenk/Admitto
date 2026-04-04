## Purpose

Admins and team owners manage teams — the foundational organizational unit in Admitto. Teams group members and scope events. Archiving retires inactive teams without data loss.

## Requirements

### Requirement: Admin can create a team
The system SHALL allow admins to create a team with a slug, name, and email address.
Team slugs SHALL be globally unique. The slug, name, and email SHALL conform to their
respective domain value object constraints.

#### Scenario: Successfully create a team
- **WHEN** an authenticated admin creates a team with slug "acme", name "Acme Events", and email "info@acme.org"
- **THEN** the team is created with the provided details and is in an active state

#### Scenario: Reject duplicate slug on create
- **WHEN** a team with slug "acme" already exists and an admin creates another team with slug "acme"
- **THEN** the request is rejected with a duplicate slug error and no new team is created

#### Scenario: Reject invalid input on create
- **WHEN** an admin creates a team with an empty name
- **THEN** the request is rejected with a validation error indicating the name is required

---

### Requirement: Team member can view team details
The system SHALL allow team members with Crew role or above to retrieve a team's
details by slug.

#### Scenario: View team details by slug
- **WHEN** a user with Crew role requests the details of team "acme"
- **THEN** the team's slug, name, email address, and version are returned

#### Scenario: Reject unauthorized team view
- **WHEN** a user who is not a member of team "acme" requests its details
- **THEN** the request is rejected as unauthorized

---

### Requirement: Admin can list all active teams
The system SHALL allow admins to list all active teams. Archived teams SHALL be
excluded from listings by default.

#### Scenario: Admin lists all active teams
- **WHEN** an admin lists all teams and teams "acme" (active), "beta" (active), and "retired" (archived) exist
- **THEN** "acme" and "beta" are returned and "retired" is not included

---

### Requirement: Authenticated users can list their teams
The system SHALL allow authenticated users to list the teams they are a member of.
Archived teams SHALL be excluded.

#### Scenario: List my teams
- **WHEN** a user who is a member of teams "acme" and "beta" lists their teams and "gamma" exists but they are not a member
- **THEN** "acme" and "beta" are returned and "gamma" is not included

#### Scenario: Archived teams excluded from my teams list
- **WHEN** a user is a member of "acme" (active) and "beta" (archived) and lists their teams
- **THEN** only "acme" is returned

---

### Requirement: Team owner can update team details
The system SHALL allow team owners to update a team's slug, name, and/or email
address as a partial update. The system SHALL use optimistic concurrency (expected
version) to prevent lost updates.

#### Scenario: Update team details with partial fields
- **WHEN** an owner of team "acme" at version 1 updates the name to "Acme Corp" with expected version 1
- **THEN** the team name is changed to "Acme Corp", slug and email remain unchanged, and the version is incremented

#### Scenario: Concurrent update conflict
- **WHEN** an owner of team "acme" at version 2 submits an update with expected version 1
- **THEN** the request is rejected with a concurrency conflict error and the team is not modified

#### Scenario: Reject update of archived team
- **WHEN** an owner attempts to update the name of an archived team
- **THEN** the request is rejected because the team is archived

---

### Requirement: Team owner can archive a team
The system SHALL allow team owners to archive an active team. Archiving is a
one-way, irreversible operation. The system SHALL prevent modifications to an
archived team. The system SHALL prevent archiving a team that has active (upcoming)
ticketed events; this check SHALL be atomic with the archive operation to prevent
race conditions.

#### Scenario: Successfully archive a team
- **WHEN** an owner archives team "acme" which is active and has no active ticketed events
- **THEN** the team status is changed to archived

#### Scenario: Reject archiving an already archived team
- **WHEN** an owner attempts to archive team "acme" which is already archived
- **THEN** the request is rejected because the team is already archived

#### Scenario: Reject archiving a team with active events
- **WHEN** an owner attempts to archive team "acme" which has an upcoming ticketed event
- **THEN** the request is rejected because the team has active events and the team remains active

---

### Requirement: Archived teams block mutations and event creation
The system SHALL prevent creating ticketed events for an archived team. Both
archive and event-creation operations SHALL serialize through the Team aggregate's
concurrency token to prevent race conditions.

#### Scenario: Reject creating an event for an archived team
- **WHEN** an organizer attempts to create a ticketed event for an archived team
- **THEN** the request is rejected because the team is archived and no event is created

#### Scenario: Concurrent archive and event creation are serialized
- **WHEN** an owner archives team "acme" and an organizer simultaneously creates an event for team "acme"
- **THEN** exactly one operation succeeds and the other is rejected with a concurrency conflict and the system remains in a consistent state
