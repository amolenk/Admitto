## Purpose

Admins and team owners manage teams — the foundational organizational unit in Admitto. Teams group members and scope events. Archiving retires inactive teams without data loss.

## Requirements

### Requirement: Admin can create a team
The system SHALL allow admins to create a team with a name and a default accent color.
The name SHALL conform to its domain value object constraints.
A `TeamId` (UUID) is assigned by the system on creation.

#### Scenario: Successfully create a team
- **WHEN** an authenticated admin creates a team with name "Acme Events"
- **THEN** the team is created with the provided name, a default accent color, is in an active state, and a `TeamId` UUID is returned

#### Scenario: Reject invalid input on create
- **WHEN** an admin creates a team with an empty name
- **THEN** the request is rejected with a validation error indicating the name is required

---

### Requirement: Team member can view team details
The system SHALL allow team members with Crew role or above to retrieve a team's
details by team ID.

#### Scenario: View team details by ID
- **WHEN** a user with Crew role requests the details of team with ID "11111111-0000-0000-0000-000000000001"
- **THEN** the team's ID, name, accent color, optional reply-to email address, and version are returned

#### Scenario: Reject unauthorized team view
- **WHEN** a user who is not a member of the requested team requests its details
- **THEN** the request is rejected as unauthorized

---

### Requirement: Admin can list all active teams
The system SHALL allow admins to list all active teams. Archived teams SHALL be
excluded from listings by default. Teams SHALL be returned in alphabetical order
by name (case-insensitive).

#### Scenario: Admin lists all active teams
- **WHEN** an admin lists all teams and teams "acme" (active), "beta" (active), and "retired" (archived) exist
- **THEN** "acme" and "beta" are returned and "retired" is not included

#### Scenario: Admin team list is ordered alphabetically
- **WHEN** an admin lists all teams and active teams "Zebra Events", "acme", and "Beta Corp" exist
- **THEN** the teams are returned in the order "acme", "Beta Corp", "Zebra Events"

---

### Requirement: Authenticated users can list their teams
The system SHALL allow authenticated users to list the teams they are a member of.
Archived teams SHALL be excluded. Teams SHALL be returned in alphabetical order
by name (case-insensitive).

#### Scenario: List my teams
- **WHEN** a user who is a member of teams "acme" and "beta" lists their teams and "gamma" exists but they are not a member
- **THEN** "acme" and "beta" are returned and "gamma" is not included

#### Scenario: Archived teams excluded from my teams list
- **WHEN** a user is a member of "acme" (active) and "beta" (archived) and lists their teams
- **THEN** only "acme" is returned

#### Scenario: My teams list is ordered alphabetically
- **WHEN** a user is a member of teams "Zebra Events", "acme", and "Beta Corp" and lists their teams
- **THEN** the teams are returned in the order "acme", "Beta Corp", "Zebra Events"

---

### Requirement: Team owner can update team details
The system SHALL allow team owners to update a team's name, accent color, and optional reply-to email address as a partial update.
The system SHALL use optimistic concurrency (expected version) to prevent lost updates.

#### Scenario: Update team details with partial fields
- **WHEN** an owner of team "Acme Events" at version 1 updates the name to "Acme Corp" with expected version 1
- **THEN** the team name is changed to "Acme Corp" and the version is incremented

#### Scenario: Team owner updates accent color
- **WHEN** an owner of team "acme" updates the accent color to `#0f766e` with the correct expected version
- **THEN** the team stores `#0f766e` and increments its version

#### Scenario: Team owner updates reply-to email address
- **WHEN** an owner of team "acme" updates the reply-to email address to `help@example.com` with the correct expected version
- **THEN** the team stores `help@example.com` and increments its version

#### Scenario: Team owner clears reply-to email address
- **WHEN** an owner of team "acme" clears the reply-to email address with the correct expected version
- **THEN** the team stores no reply-to email address and increments its version

#### Scenario: Invalid accent color is rejected
- **WHEN** an owner updates the team accent color to `not-a-color`
- **THEN** the request is rejected with a validation error and the team is unchanged

#### Scenario: Team details include accent color
- **WHEN** a team member retrieves team details
- **THEN** the response includes the team's accent color and optional reply-to email address

#### Scenario: Concurrent update conflict
- **WHEN** an owner of team "acme" at version 2 submits an update with expected version 1
- **THEN** the request is rejected with a concurrency conflict error and the team is not modified

#### Scenario: Reject update of archived team
- **WHEN** an owner attempts to update the name of an archived team
- **THEN** the request is rejected because the team is archived

---

### Requirement: Team owner can archive a team
The system SHALL allow team owners to archive an active team. Archiving is a
one-way, irreversible operation. The system SHALL prevent modifications to
an archived team. The system SHALL prevent archiving a team that has any
active (non-archived/non-cancelled) or pending ticketed events. The check
SHALL be a local invariant on the `Team` aggregate: archive is allowed only
when `ActiveEventCount == 0` **and** `PendingEventCount == 0`. Because both
the archive operation and any operation that increments those counters
serialize through the `Team` aggregate's concurrency token, no cross-module
synchronisation is required to make this check safe.

#### Scenario: Successfully archive a team
- **WHEN** an owner archives team "acme" which is active, has `ActiveEventCount = 0`, and `PendingEventCount = 0`
- **THEN** the team status is changed to archived

#### Scenario: Reject archiving an already archived team
- **WHEN** an owner attempts to archive team "acme" which is already archived
- **THEN** the request is rejected because the team is already archived

#### Scenario: Reject archiving a team with active events
- **WHEN** an owner attempts to archive team "acme" which has `ActiveEventCount = 1`
- **THEN** the request is rejected because the team has active events and the team remains active

#### Scenario: Reject archiving a team with pending events
- **WHEN** an owner attempts to archive team "acme" which has `PendingEventCount = 1`
- **THEN** the request is rejected because the team has pending event creations and the team remains active

---

### Requirement: Archived teams block mutations and event creation
The system SHALL prevent accepting event creation requests for an archived
team. Both archive and creation-request operations SHALL serialize through
the `Team` aggregate's concurrency token to prevent race conditions. Once a
creation request has been accepted and `PendingEventCount` incremented, the
team cannot be archived until the pending count returns to zero (see
"Team owner can archive a team").

#### Scenario: Reject creating an event for an archived team
- **WHEN** a team owner attempts to post a creation request for an archived team
- **THEN** the request is rejected because the team is archived and no `TeamEventCreationRequest` is created

#### Scenario: Concurrent archive and creation request are serialized
- **WHEN** an owner archives team "acme" and another owner simultaneously posts a creation request for team "acme"
- **THEN** exactly one operation succeeds and the other is rejected with a concurrency conflict, and the system remains in a consistent state

---

### Requirement: Team tracks bounded event counters
The `Team` aggregate SHALL maintain four non-negative integer counters:
`ActiveEventCount`, `CancelledEventCount`, `ArchivedEventCount`, and
`PendingEventCount`. The counters default to zero on team creation. They are
the only event-related state stored on the Organization side — no per-event
slug list or per-event entity is stored on `Team`. All counter mutations go
through the `Team` aggregate and use its concurrency token.

#### Scenario: Counters default to zero on create
- **WHEN** a new team is created
- **THEN** `ActiveEventCount`, `CancelledEventCount`, `ArchivedEventCount`, and `PendingEventCount` are all zero

#### Scenario: Counters are not negative
- **WHEN** any handler would decrement a counter below zero
- **THEN** the operation fails with an invariant-violation error

---

### Requirement: Creation request increments PendingEventCount and records a request entity
The system SHALL, when Organization accepts an event creation request from a team owner, increment `PendingEventCount` and persist a `TeamEventCreationRequest` entity under the `Team` aggregate capturing the `CreationRequestId`, the requester identity, and a `RequestedAt` timestamp. The entity SHALL start in state `Pending`. Both the counter update and the request persistence SHALL occur in the same unit of work as the `TicketedEventCreationRequested` integration event being outboxed.

#### Scenario: Accepted creation request stores a Pending entity
- **WHEN** a team owner of team "Acme Events" posts a creation request for "Conf 2026"
- **THEN** a `TeamEventCreationRequest` is stored in state `Pending` with the new `CreationRequestId`, `PendingEventCount` increases by one, and a `TicketedEventCreationRequested` event is outboxed in the same unit of work

---

### Requirement: Team counters react to Registrations integration events
The Organization module SHALL consume the `TicketedEvent*` integration events
published by Registrations and advance the team's counters and request state
in response. All handlers SHALL be idempotent with respect to redelivery by
keying off the `CreationRequestId` (for creation responses) or the
`TicketedEventId` (for lifecycle events) and using the current state of the
`TeamEventCreationRequest` / counter values as the idempotency guard.

The specific reactions:

- **`TicketedEventCreated`** (carrying `CreationRequestId`, `TicketedEventId`):
  mark the matching `TeamEventCreationRequest` as `Created` (storing the
  `TicketedEventId`), decrement `PendingEventCount` by one, and
  increment `ActiveEventCount` by one.
- **`TicketedEventCreationRejected`** (carrying `CreationRequestId`, reason):
  mark the matching `TeamEventCreationRequest` as `Rejected` (storing the
  reason), and decrement `PendingEventCount` by one.
- **`TicketedEventCancelled`** (carrying `TicketedEventId`): decrement
  `ActiveEventCount` by one and increment `CancelledEventCount` by one.
- **`TicketedEventArchived`** (carrying `TicketedEventId`): if the event was
  previously Active, decrement `ActiveEventCount`; if previously Cancelled,
  decrement `CancelledEventCount`. Increment `ArchivedEventCount` by one.

#### Scenario: Successful creation advances counters
- **WHEN** Organization processes `TicketedEventCreated` for `CreationRequestId = R1` on team "acme" with `PendingEventCount = 1`, `ActiveEventCount = 0`
- **THEN** the matching `TeamEventCreationRequest` is `Created`, `PendingEventCount` becomes 0, and `ActiveEventCount` becomes 1

#### Scenario: Rejected creation rolls back pending
- **WHEN** Organization processes `TicketedEventCreationRejected` for `CreationRequestId = R2` on team "Acme Events" with `PendingEventCount = 1`
- **THEN** the matching `TeamEventCreationRequest` is `Rejected` and `PendingEventCount` becomes 0

#### Scenario: Cancellation moves counter from active to cancelled
- **WHEN** Organization processes `TicketedEventCancelled` for a team whose event was Active
- **THEN** `ActiveEventCount` decreases by one and `CancelledEventCount` increases by one

#### Scenario: Archive from active
- **WHEN** Organization processes `TicketedEventArchived` for an event that was Active
- **THEN** `ActiveEventCount` decreases by one and `ArchivedEventCount` increases by one

#### Scenario: Archive from cancelled
- **WHEN** Organization processes `TicketedEventArchived` for an event that was Cancelled
- **THEN** `CancelledEventCount` decreases by one and `ArchivedEventCount` increases by one

#### Scenario: Redelivered creation-success is idempotent
- **WHEN** `TicketedEventCreated` for `CreationRequestId = R1` is delivered a second time and the request is already `Created`
- **THEN** the counters are not changed again

---

### Requirement: Stale creation requests expire
The system SHALL expire `TeamEventCreationRequest` entities that remain in
state `Pending` longer than a configurable timeout (default 24 hours). A
Quartz-scheduled job SHALL transition such requests to state `Expired` and
decrement `PendingEventCount` accordingly. Expired requests SHALL be visible
on the creation-status endpoint (see event-management).

This prevents `PendingEventCount` drift if a `TicketedEventCreationRequested`
integration event is permanently unprocessable in Registrations.

#### Scenario: Expiring a stuck request
- **WHEN** a `TeamEventCreationRequest` has been in `Pending` for longer than the configured timeout
- **THEN** the job marks it `Expired` and decrements `PendingEventCount` by one

#### Scenario: Expired request is visible on the status endpoint
- **WHEN** a `TeamEventCreationRequest` has been expired
- **THEN** `GET` on its creation-status URL returns status `Expired`
