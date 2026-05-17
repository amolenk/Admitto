## REMOVED Requirements

### Requirement: Organizer can cancel a ticket type
**Reason**: The cancel-ticket-type operation is removed. Ticket types are active until the event is archived. Organizers who need to stop selling a ticket type should archive the event (after notifying attendees via bulk email).
**Migration**: Remove the `POST …/ticket-types/{id}/cancel` endpoint. Remove the `cancellationStatus` field from ticket type responses. Any existing rows with `status = Cancelled` in `ticket_types` SHALL be migrated to `Active` in the EF migration.

---

## MODIFIED Requirements

### Requirement: Organizer can add a ticket type
The system SHALL allow organizers to add a ticket type to a `TicketCatalog` when `TicketCatalog.EventStatus` is `Active`. The ticket type SHALL be rejected when `EventStatus` is `Archived`.

#### Scenario: Add a ticket type when event is Active
- **WHEN** event "conf-2026" has `TicketCatalog.EventStatus` Active and an organizer adds a ticket type "General Admission" with capacity 100
- **THEN** the ticket type is created and listed for the event

#### Scenario: Reject adding ticket type when event is Archived
- **WHEN** event "conf-2026" has `TicketCatalog.EventStatus` Archived and an organizer attempts to add a ticket type
- **THEN** the request is rejected

---

### Requirement: Organizer can update a ticket type
The system SHALL allow organizers to update an existing ticket type when `TicketCatalog.EventStatus` is `Active`. Updates SHALL be rejected when `EventStatus` is `Archived`.

#### Scenario: Update a ticket type
- **WHEN** `TicketCatalog.EventStatus` is Active and an organizer updates ticket type name to "General Admission v2"
- **THEN** the ticket type is updated

#### Scenario: Reject update when event is Archived
- **WHEN** `TicketCatalog.EventStatus` is Archived and an organizer attempts to update a ticket type
- **THEN** the request is rejected

---

### Requirement: Organizer can list ticket types
The system SHALL allow Crew members to list all ticket types for an event. Each ticket type SHALL include its `id`, name, time slots, capacity (max and used), and whether self-service registration is enabled. There is no `cancellationStatus` field.

#### Scenario: List ticket types
- **WHEN** a Crew member lists ticket types for event "conf-2026" which has "General Admission" (capacity 100/50 used) and "VIP Pass" (capacity 50/10 used)
- **THEN** both ticket types are returned with their id, name, capacity details, and self-service status

---

### Requirement: TicketCatalog projects only Active and Archived event status
The `TicketCatalog` aggregate SHALL track the owning event's lifecycle through an `EventStatus` field that mirrors the `TicketedEvent` status. Valid values are `Active` and `Archived` only. The `Cancelled` status is removed. The state transition is one-way: `Active → Archived`. The catalog SHALL reject ticket-type mutations when `EventStatus` is Archived.

#### Scenario: Archive is projected in the same unit of work
- **WHEN** an organizer archives a `TicketedEvent`
- **THEN** the `TicketedEvent` becomes `Archived` and `TicketCatalog.EventStatus` becomes `Archived` in the same database transaction

#### Scenario: Claim refused for archived event
- **WHEN** the registration handler invokes `TicketCatalog.Claim` for a catalog whose `EventStatus = Archived`
- **THEN** the claim is rejected
