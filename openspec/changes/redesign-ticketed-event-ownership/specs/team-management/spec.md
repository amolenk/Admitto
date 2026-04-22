## MODIFIED Requirements

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
- **WHEN** an organizer attempts to post a creation request for an archived team
- **THEN** the request is rejected because the team is archived and no `TeamEventCreationRequest` is created

#### Scenario: Concurrent archive and creation request are serialized
- **WHEN** an owner archives team "acme" and an organizer simultaneously posts a creation request for team "acme"
- **THEN** exactly one operation succeeds and the other is rejected with a concurrency conflict, and the system remains in a consistent state

## ADDED Requirements

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
The system SHALL, when Organization accepts an event creation request from an organizer, increment `PendingEventCount` and persist a `TeamEventCreationRequest` entity under the `Team` aggregate capturing the `CreationRequestId`, the requested slug, the requester identity, and a `RequestedAt` timestamp. The entity SHALL start in state `Pending`. Both the counter update and the request persistence SHALL occur in the same unit of work as the `TicketedEventCreationRequested` integration event being outboxed.

#### Scenario: Accepted creation request stores a Pending entity
- **WHEN** an organizer of team "acme" posts a creation request for slug "conf-2026"
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

- **`TicketedEventCreated`** (carrying `CreationRequestId`, `TicketedEventId`, slug):
  mark the matching `TeamEventCreationRequest` as `Created` (storing the
  `TicketedEventId` and slug), decrement `PendingEventCount` by one, and
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
- **WHEN** Organization processes `TicketedEventCreationRejected` for `CreationRequestId = R2` on team "acme" with `PendingEventCount = 1` with reason "duplicate_slug"
- **THEN** the matching `TeamEventCreationRequest` is `Rejected` with reason `duplicate_slug` and `PendingEventCount` becomes 0

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
