# Ticket Type Management Specification

## Purpose

Organizers add, update, cancel, and list ticket types for an event. Ticket types live on the event's `TicketCatalog` aggregate, which projects the `TicketedEvent` lifecycle status as `EventStatus` so ticket-type mutations and capacity claims can be gated locally without cross-aggregate reads.

## Requirements

### Requirement: Organizer can add a ticket type to an event
The system SHALL allow organizers (Owner or Organizer role) to add a ticket type to
an event with a name, time slots, optional capacity, and a `SelfServiceEnabled`
flag (defaults to `true`). The server SHALL generate a `TicketTypeId` (GUID) upon
creation. Ticket type names SHALL be unique within an event (case-insensitive).
Adding a ticket type mutates the event's `TicketCatalog`: the command is rejected
when `TicketCatalog.EventStatus` is Archived, and succeeds only when Active. The `TicketCatalog` is
created by the Registrations module's reaction to `TicketedEventCreated`, so it
already exists by the time any ticket-type command can run; there is no longer a
"create catalog on first ticket type" path.

#### Scenario: Add a ticket type to an active event
- **WHEN** an organizer adds a ticket type with name "VIP Pass", time slots ["Morning Session", "Afternoon Session"], and capacity 100 to event "conf-2026" whose `TicketCatalog.EventStatus` is Active
- **THEN** the event has a ticket type with name "VIP Pass", the provided details, used capacity 0, and a server-assigned `id`

#### Scenario: Add a ticket type with no capacity
- **WHEN** an organizer adds a ticket type with name "Speaker Pass" and no capacity to event "conf-2026" whose `TicketCatalog.EventStatus` is Active
- **THEN** the event has a ticket type "Speaker Pass" with no capacity set

#### Scenario: Reject duplicate ticket type name
- **WHEN** event "conf-2026" already has a ticket type with name "VIP Pass" and an organizer adds another with name "VIP Pass" (or "vip pass" — case-insensitive)
- **THEN** the request is rejected with a duplicate ticket type name error

#### Scenario: Reject adding ticket type when event is Archived
- **WHEN** event "conf-2026" has `TicketCatalog.EventStatus` Archived and an organizer attempts to add a ticket type
- **THEN** the request is rejected with reason "event not active"

#### Scenario: Add a self-service-enabled ticket type
- **WHEN** an organizer adds a ticket type with name "General Admission", capacity 200, and `selfServiceEnabled: true` to event "conf-2026"
- **THEN** the ticket type is created with `SelfServiceEnabled = true`

#### Scenario: Add an admin-only ticket type
- **WHEN** an organizer adds a ticket type with name "VIP Pass", capacity 50, and `selfServiceEnabled: false` to event "conf-2026"
- **THEN** the ticket type is created with `SelfServiceEnabled = false` and self-service registration for this ticket type is rejected

---

### Requirement: Organizer can update a ticket type
The system SHALL allow organizers to update a ticket type's name, capacity, and
`SelfServiceEnabled` flag, identified by its `TicketTypeId`. Updating a ticket type
SHALL be rejected when `TicketCatalog.EventStatus` is not Active. Optimistic
concurrency on the `TicketCatalog` row is sufficient to detect concurrent
status transitions; no separate mutation counter is maintained.

#### Scenario: Update a ticket type's capacity
- **WHEN** an organizer updates ticket type with id {tt-id} to capacity 200 on an event whose `TicketCatalog.EventStatus` is Active
- **THEN** the ticket type capacity is changed to 200

#### Scenario: Update a ticket type's name
- **WHEN** an organizer updates ticket type with id {tt-id} name to "VIP Access" on an event whose `TicketCatalog.EventStatus` is Active
- **THEN** the ticket type name is updated

#### Scenario: Reject update when event is Archived
- **WHEN** `TicketCatalog.EventStatus` is Archived and an organizer attempts to update a ticket type
- **THEN** the request is rejected with reason "event not active"

#### Scenario: Disable self-service on an existing ticket type
- **WHEN** an organizer updates ticket type with id {tt-id} setting `selfServiceEnabled: false` on an active event
- **THEN** the ticket type's `SelfServiceEnabled` becomes `false` and subsequent self-service registrations for it are rejected

#### Scenario: Re-enable self-service on a ticket type
- **WHEN** an organizer updates ticket type with id {tt-id} setting `selfServiceEnabled: true` on an active event
- **THEN** the ticket type's `SelfServiceEnabled` becomes `true` and self-service registrations for it are accepted

---

### Requirement: Organizer can list ticket types
The system SHALL allow team members with Crew role or above to list all ticket types
for an event. Each ticket type SHALL include its
`id`, name, time slots, capacity (max and used), and
`selfServiceEnabled` flag. There is no `cancellationStatus` field.

#### Scenario: List ticket types for an event
- **WHEN** a Crew member lists ticket types for event "conf-2026" which has "General Admission" (capacity 100/50 used) and "VIP Pass" (capacity 50/10 used)
- **THEN** both ticket types are returned with their id, name, capacity details, and self-service status

#### Scenario: List ticket types for an event with no ticket types
- **WHEN** a Crew member lists ticket types for event "conf-2026" which has no ticket types
- **THEN** an empty list is returned

#### Scenario: List ticket types includes selfServiceEnabled
- **WHEN** a Crew member lists ticket types for event "conf-2026" which has "General Admission" (selfServiceEnabled: true) and "VIP Pass" (selfServiceEnabled: false)
- **THEN** both ticket types are returned with their respective `selfServiceEnabled` values

---

### Requirement: TicketCatalog projects only Active and Archived event status
The `TicketCatalog` aggregate SHALL hold an `EventStatus` field with values
`Active` or `Archived` only. The `Cancelled` status is removed. The field SHALL be initialised to `Active`
when the catalog is created in response to `TicketedEventCreated`. The state
transition is one-way: `Active → Archived`. The catalog SHALL reject ticket-type
mutations when `EventStatus` is `Archived`.

The `EventStatus` is the only event-level state the catalog stores; all richer
event details (policies, name, dates) remain on `TicketedEvent` and are read
directly from there by application handlers.

#### Scenario: Catalog is created Active
- **WHEN** Registrations processes its own `TicketedEventCreated` domain event
- **THEN** a `TicketCatalog` is created for the event with `EventStatus = Active`

#### Scenario: Archive is projected in the same unit of work
- **WHEN** an organizer archives a `TicketedEvent`
- **THEN** the `TicketedEvent` becomes `Archived` and `TicketCatalog.EventStatus` becomes `Archived` in the same database transaction

#### Scenario: Claim refused for archived event
- **WHEN** the registration handler invokes `TicketCatalog.Claim` for a catalog whose `EventStatus = Archived`
- **THEN** the claim is rejected

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

---

### Requirement: Public endpoint lists self-service-enabled ticket types
The system SHALL expose a public endpoint `GET /events/{teamId}/{eventId}/ticket-types`
that requires API key authentication and returns all active (not cancelled),
self-service-enabled ticket types for the event. This endpoint is intended for
external websites to determine which ticket types to present to attendees.
Cancelled ticket types and ticket types with `SelfServiceEnabled = false` SHALL
be excluded from the response. Each ticket type in the response SHALL include:
`id`, name, time slots, max capacity (null if unlimited), and used capacity.

#### Scenario: Returns only self-service-enabled, active ticket types
- **GIVEN** an event has "General Admission" (selfServiceEnabled: true, active), "VIP Pass" (selfServiceEnabled: false, active), and "Early Bird" (selfServiceEnabled: true, cancelled)
- **WHEN** an external caller fetches `GET /events/{teamId}/{eventId}/ticket-types`
- **THEN** only "General Admission" is returned (VIP Pass is admin-only, Early Bird is cancelled)

#### Scenario: Returns empty list when no self-service ticket types exist
- **GIVEN** an event has only admin-only ticket types
- **WHEN** an external caller fetches `GET /events/{teamId}/{eventId}/ticket-types`
- **THEN** an empty list is returned

#### Scenario: Returns 404 when event does not exist
- **WHEN** an external caller fetches ticket types for a non-existent team ID or event ID
- **THEN** the response is HTTP 404 Not Found

