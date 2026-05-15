## MODIFIED Requirements

### Requirement: Organizer can add a ticket type to an event
The system SHALL allow organizers (Owner or Organizer role) to add a ticket type to
an event with a name, time slots, optional capacity, and a `SelfServiceEnabled`
flag (defaults to `true`). The server SHALL generate a `TicketTypeId` (GUID) upon
creation. Ticket type names SHALL be unique within an event (case-insensitive).
Adding a ticket type mutates the event's `TicketCatalog`: the command is rejected
when `TicketCatalog.EventStatus` is Cancelled or Archived, and succeeds only when Active. The `TicketCatalog` is
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

#### Scenario: Reject adding ticket type when event is Cancelled
- **WHEN** event "conf-2026" has `TicketCatalog.EventStatus` Cancelled and an organizer attempts to add a ticket type
- **THEN** the request is rejected with reason "event not active"

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

#### Scenario: Reject update when event is Cancelled
- **WHEN** `TicketCatalog.EventStatus` is Cancelled and an organizer attempts to update a ticket type
- **THEN** the request is rejected with reason "event not active"

#### Scenario: Concurrent cancel detected via optimistic concurrency
- **WHEN** an organizer submits an update against a `TicketCatalog` whose `EventStatus` was just transitioned to Cancelled by an in-flight projection from `TicketedEvent`
- **THEN** the update fails with a concurrency conflict and no change is persisted

#### Scenario: Disable self-service on an existing ticket type
- **WHEN** an organizer updates ticket type with id {tt-id} setting `selfServiceEnabled: false` on an active event
- **THEN** the ticket type's `SelfServiceEnabled` becomes `false` and subsequent self-service registrations for it are rejected

#### Scenario: Re-enable self-service on a ticket type
- **WHEN** an organizer updates ticket type with id {tt-id} setting `selfServiceEnabled: true` on an active event
- **THEN** the ticket type's `SelfServiceEnabled` becomes `true` and self-service registrations for it are accepted

---

### Requirement: Organizer can cancel a ticket type
The system SHALL allow organizers to cancel an active ticket type (identified by its
`TicketTypeId`), preventing new registrations for it. The system SHALL reject
cancelling an already cancelled ticket type. Cancelling a ticket type SHALL be
rejected when `TicketCatalog.EventStatus` is not Active.

#### Scenario: Cancel a ticket type
- **WHEN** an organizer cancels active ticket type with id {tt-id} on event "conf-2026" whose `TicketCatalog.EventStatus` is Active
- **THEN** the ticket type is marked as cancelled and no new registrations can be made for it

#### Scenario: Reject cancelling an already cancelled ticket type
- **WHEN** an organizer attempts to cancel a ticket type which is already cancelled
- **THEN** the request is rejected because the ticket type is already cancelled

#### Scenario: Reject cancelling ticket type when event is Cancelled
- **WHEN** `TicketCatalog.EventStatus` is Cancelled and an organizer attempts to cancel a ticket type
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Team member can list ticket types for an event
The system SHALL allow team members with Crew role or above to list all ticket types
for an event, including cancelled ticket types. Each ticket type SHALL include its
`id`, name, time slots, capacity (max and used), cancellation status, and
`selfServiceEnabled` flag.

#### Scenario: List ticket types for an event
- **WHEN** a Crew member lists ticket types for event "conf-2026" which has "General Admission" (active, capacity 100/50 used), "VIP Pass" (active, capacity 50/10 used), and "Early Bird" (cancelled)
- **THEN** all three ticket types are returned with their id, name, capacity details, and cancellation status

#### Scenario: List ticket types for an event with no ticket types
- **WHEN** a Crew member lists ticket types for event "conf-2026" which has no ticket types
- **THEN** an empty list is returned

#### Scenario: List ticket types includes selfServiceEnabled
- **WHEN** a Crew member lists ticket types for event "conf-2026" which has "General Admission" (selfServiceEnabled: true) and "VIP Pass" (selfServiceEnabled: false)
- **THEN** both ticket types are returned with their respective `selfServiceEnabled` values

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
