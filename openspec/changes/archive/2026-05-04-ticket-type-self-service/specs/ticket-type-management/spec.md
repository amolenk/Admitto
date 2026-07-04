## MODIFIED Requirements

### Requirement: Organizer can add a ticket type to an event
The system SHALL allow organizers (Owner or Organizer role) to add a ticket type to
an event with a slug, name, time slots, optional capacity, and a `SelfServiceEnabled`
flag (defaults to `true`). Ticket type slugs SHALL be unique within an event.
Adding a ticket type mutates the event's `TicketCatalog`: the command is rejected
when `TicketCatalog.EventStatus` is Cancelled or Archived, and succeeds only when Active.

#### Scenario: Add a self-service-enabled ticket type
- **WHEN** an organizer adds a ticket type with slug "general", name "General Admission", capacity 200, and `selfServiceEnabled: true` to event "conf-2026"
- **THEN** the ticket type is created with `SelfServiceEnabled = true`

#### Scenario: Add an admin-only ticket type
- **WHEN** an organizer adds a ticket type with slug "vip", name "VIP Pass", capacity 50, and `selfServiceEnabled: false` to event "conf-2026"
- **THEN** the ticket type is created with `SelfServiceEnabled = false` and self-service registration for this ticket type is rejected

---

### Requirement: Organizer can update a ticket type
The system SHALL allow organizers to update a ticket type's name, capacity, and
`SelfServiceEnabled` flag. Ticket type slugs SHALL be immutable after creation.
Updating a ticket type SHALL be rejected when `TicketCatalog.EventStatus` is not Active.

#### Scenario: Disable self-service on an existing ticket type
- **WHEN** an organizer updates ticket type "general" setting `selfServiceEnabled: false` on an active event
- **THEN** the ticket type's `SelfServiceEnabled` becomes `false` and subsequent self-service registrations for it are rejected

#### Scenario: Re-enable self-service on a ticket type
- **WHEN** an organizer updates ticket type "vip" setting `selfServiceEnabled: true` on an active event
- **THEN** the ticket type's `SelfServiceEnabled` becomes `true` and self-service registrations for it are accepted

---

### Requirement: Team member can list ticket types for an event
The system SHALL allow team members with Crew role or above to list all ticket types
for an event, including cancelled ticket types. Each ticket type SHALL include its
slug, name, time slots, capacity (max and used), cancellation status, and
`selfServiceEnabled` flag.

#### Scenario: List ticket types includes selfServiceEnabled
- **WHEN** a Crew member lists ticket types for event "conf-2026" which has "general" (selfServiceEnabled: true) and "vip" (selfServiceEnabled: false)
- **THEN** both ticket types are returned with their respective `selfServiceEnabled` values

---

## ADDED Requirements

### Requirement: Public endpoint lists self-service-enabled ticket types
The system SHALL expose a public endpoint `GET /events/{teamSlug}/{eventSlug}/ticket-types`
that requires API key authentication and returns all active (not cancelled),
self-service-enabled ticket types for the event. This endpoint is intended for
external websites to determine which ticket types to present to attendees.
Cancelled ticket types and ticket types with `SelfServiceEnabled = false` SHALL
be excluded from the response. Each ticket type in the response SHALL include:
slug, name, time slots, max capacity (null if unlimited), and used capacity.

#### Scenario: Returns only self-service-enabled, active ticket types
- **GIVEN** event "conf-2026" has "general" (selfServiceEnabled: true, active), "vip" (selfServiceEnabled: false, active), and "early-bird" (selfServiceEnabled: true, cancelled)
- **WHEN** an external caller fetches `GET /events/acme/conf-2026/ticket-types`
- **THEN** only "general" is returned (vip is admin-only, early-bird is cancelled)

#### Scenario: Returns empty list when no self-service ticket types exist
- **GIVEN** event "conf-2026" has only admin-only ticket types
- **WHEN** an external caller fetches `GET /events/acme/conf-2026/ticket-types`
- **THEN** an empty list is returned

#### Scenario: Returns 404 when event does not exist
- **WHEN** an external caller fetches ticket types for a non-existent event or team
- **THEN** the response is HTTP 404 Not Found
