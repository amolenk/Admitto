# Ticket Type Management Specification

## Purpose

Organizers add, update, cancel, and list ticket types for an event. Ticket types live on the event's `TicketCatalog` aggregate, which projects the `TicketedEvent` lifecycle status as `EventStatus` so ticket-type mutations and capacity claims can be gated locally without cross-aggregate reads.

## Requirements

### Requirement: Organizer can add a ticket type to an event
The system SHALL allow organizers (Owner or Organizer role) to add a ticket type to
an event with a slug, name, time slots, and optional capacity. Ticket type slugs
SHALL be unique within an event. Adding a ticket type mutates the event's
`TicketCatalog`: the command is rejected when `TicketCatalog.EventStatus` is
Cancelled or Archived, and succeeds only when Active. The `TicketCatalog` is
created by the Registrations module's reaction to `TicketedEventCreated`, so it
already exists by the time any ticket-type command can run; there is no longer a
"create catalog on first ticket type" path.

#### Scenario: Add a ticket type to an active event
- **WHEN** an organizer adds a ticket type with slug "vip", name "VIP Pass", time slots ["morning", "afternoon"], and capacity 100 to event "conf-2026" whose `TicketCatalog.EventStatus` is Active
- **THEN** the event has a ticket type "vip" with the provided details and used capacity 0

#### Scenario: Add a ticket type with no capacity
- **WHEN** an organizer adds a ticket type with slug "speaker", name "Speaker Pass", and no capacity to event "conf-2026" whose `TicketCatalog.EventStatus` is Active
- **THEN** the event has a ticket type "speaker" with no capacity set

#### Scenario: Reject duplicate ticket type slug
- **WHEN** event "conf-2026" already has a ticket type with slug "vip" and an organizer adds another with slug "vip"
- **THEN** the request is rejected with a duplicate ticket type slug error

#### Scenario: Reject adding ticket type when event is Cancelled
- **WHEN** event "conf-2026" has `TicketCatalog.EventStatus` Cancelled and an organizer attempts to add a ticket type
- **THEN** the request is rejected with reason "event not active"

#### Scenario: Reject adding ticket type when event is Archived
- **WHEN** event "conf-2026" has `TicketCatalog.EventStatus` Archived and an organizer attempts to add a ticket type
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Organizer can update a ticket type
The system SHALL allow organizers to update a ticket type's name and capacity.
Ticket type slugs SHALL be immutable after creation. Updating a ticket type
SHALL be rejected when `TicketCatalog.EventStatus` is not Active. Optimistic
concurrency on the `TicketCatalog` row is sufficient to detect concurrent
status transitions; no separate mutation counter is maintained.

#### Scenario: Update a ticket type's capacity
- **WHEN** an organizer updates ticket type "vip" to capacity 200 on an event whose `TicketCatalog.EventStatus` is Active
- **THEN** the ticket type capacity is changed to 200

#### Scenario: Update a ticket type's name
- **WHEN** an organizer updates ticket type "vip" name to "VIP Access" on an event whose `TicketCatalog.EventStatus` is Active
- **THEN** the ticket type name is updated

#### Scenario: Reject update when event is Cancelled
- **WHEN** `TicketCatalog.EventStatus` is Cancelled and an organizer attempts to update a ticket type
- **THEN** the request is rejected with reason "event not active"

#### Scenario: Concurrent cancel detected via optimistic concurrency
- **WHEN** an organizer submits an update against a `TicketCatalog` whose `EventStatus` was just transitioned to Cancelled by an in-flight projection from `TicketedEvent`
- **THEN** the update fails with a concurrency conflict and no change is persisted

---

### Requirement: Organizer can cancel a ticket type
The system SHALL allow organizers to cancel an active ticket type, preventing new
registrations for it. The system SHALL reject cancelling an already cancelled ticket
type. Cancelling a ticket type SHALL be rejected when
`TicketCatalog.EventStatus` is not Active.

#### Scenario: Cancel a ticket type
- **WHEN** an organizer cancels active ticket type "vip" on event "conf-2026" whose `TicketCatalog.EventStatus` is Active
- **THEN** the ticket type is marked as cancelled and no new registrations can be made for it

#### Scenario: Reject cancelling an already cancelled ticket type
- **WHEN** an organizer attempts to cancel ticket type "early-bird" which is already cancelled
- **THEN** the request is rejected because the ticket type is already cancelled

#### Scenario: Reject cancelling ticket type when event is Cancelled
- **WHEN** `TicketCatalog.EventStatus` is Cancelled and an organizer attempts to cancel a ticket type
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Team member can list ticket types for an event
The system SHALL allow team members with Crew role or above to list all ticket types
for an event, including cancelled ticket types. Each ticket type SHALL include its
slug, name, time slots, capacity (max and used), and cancellation status.

#### Scenario: List ticket types for an event
- **WHEN** a Crew member lists ticket types for event "conf-2026" which has "general" (active, capacity 100/50 used), "vip" (active, capacity 50/10 used), and "early-bird" (cancelled)
- **THEN** all three ticket types are returned with their slug, name, capacity details, and cancellation status

#### Scenario: List ticket types for an event with no ticket types
- **WHEN** a Crew member lists ticket types for event "conf-2026" which has no ticket types
- **THEN** an empty list is returned

---

### Requirement: TicketCatalog projects the TicketedEvent status
The `TicketCatalog` aggregate SHALL hold an `EventStatus` field with values
`Active`, `Cancelled`, or `Archived`. The field SHALL be initialised to `Active`
when the catalog is created in response to `TicketedEventCreated`. Subsequent
`TicketedEvent` lifecycle changes (cancel, archive) SHALL be projected onto the
catalog via an in-module domain event handled in the **same unit of work** as
the `TicketedEvent` mutation that triggered it. Status transitions on the
catalog SHALL be one-way: `Active → Cancelled`, `Active → Archived`, and
`Cancelled → Archived`.

The `EventStatus` is the only event-level state the catalog stores; all richer
event details (policies, name, dates) remain on `TicketedEvent` and are read
directly from there by application handlers.

#### Scenario: Catalog is created Active
- **WHEN** Registrations processes its own `TicketedEventCreated` domain event
- **THEN** a `TicketCatalog` is created for the event with `EventStatus = Active`

#### Scenario: Cancellation is projected in the same unit of work
- **WHEN** an organizer cancels a `TicketedEvent`
- **THEN** the `TicketedEvent` becomes `Cancelled` and `TicketCatalog.EventStatus` becomes `Cancelled` in the same database transaction

#### Scenario: Archive from cancelled
- **WHEN** an organizer archives a `TicketedEvent` whose status is Cancelled
- **THEN** both `TicketedEvent.Status` and `TicketCatalog.EventStatus` become `Archived` in the same transaction

#### Scenario: Reject illegal transition
- **WHEN** any code path attempts to transition `TicketCatalog.EventStatus` from `Archived` back to `Active`
- **THEN** the operation fails with an invariant-violation error

---

### Requirement: TicketCatalog claim is gated by EventStatus
`TicketCatalog.Claim(...)` SHALL refuse to consume capacity when
`EventStatus` is Cancelled or Archived, returning a domain error that
application handlers translate into a "event not active" rejection. This is the
authoritative gate for atomic registration: even if a registration handler's
prior `TicketedEvent` policy check observed Active, the claim against the
catalog SHALL fail when a concurrent cancel/archive has been projected before
the claim's commit. Optimistic concurrency on the `TicketCatalog` row provides
the safety net.

#### Scenario: Claim succeeds for active event
- **WHEN** the registration handler invokes `TicketCatalog.Claim` for a catalog with `EventStatus = Active` and sufficient capacity
- **THEN** capacity is consumed and the claim succeeds

#### Scenario: Claim refused for cancelled event
- **WHEN** the registration handler invokes `TicketCatalog.Claim` for a catalog whose `EventStatus = Cancelled`
- **THEN** the claim is refused with reason "event not active" and no capacity is consumed

#### Scenario: Concurrent cancel projected between policy check and claim
- **WHEN** the registration handler's policy check passed against `TicketedEvent` (Active) but the cancellation projection commits and updates `TicketCatalog.EventStatus` to Cancelled before the claim's commit
- **THEN** the claim fails (status check or optimistic concurrency conflict) and no capacity is consumed
