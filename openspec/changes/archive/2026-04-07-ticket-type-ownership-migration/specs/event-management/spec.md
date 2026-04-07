## REMOVED Requirements

### Requirement: Organizer can add ticket types to an event
**Reason**: Ticket type management has moved from the Organization module to the Registrations module as part of the ticket-type-management capability.
**Migration**: Use the ticket-type-management capability endpoints. URL patterns remain unchanged (`/admin/teams/{teamSlug}/events/{eventSlug}/ticket-types`).

### Requirement: Organizer can update ticket types
**Reason**: Ticket type management has moved from the Organization module to the Registrations module as part of the ticket-type-management capability.
**Migration**: Use the ticket-type-management capability endpoints. URL patterns remain unchanged.

### Requirement: Organizer can cancel ticket types
**Reason**: Ticket type management has moved from the Organization module to the Registrations module as part of the ticket-type-management capability.
**Migration**: Use the ticket-type-management capability endpoints. URL patterns remain unchanged.

### Requirement: Capacity tracking is synchronized with ticket type changes
**Reason**: Capacity is now tracked natively within the Registrations module's ticket catalog aggregate. Cross-module capacity sync via `TicketTypeAddedModuleEvent` and `TicketTypeCapacityChangedModuleEvent` is no longer needed.
**Migration**: No migration needed. Capacity tracking is automatic as part of ticket type management in the Registrations module.

---

## MODIFIED Requirements

### Requirement: Team member can view event details
The system SHALL allow team members with Crew role or above to view a ticketed
event's details by event slug. Ticket types are managed separately by the
Registrations module and are not included in the event details response.

#### Scenario: View event details
- **WHEN** a Crew member of team "acme" views event "conf-2026"
- **THEN** the event's name, dates, URLs, and status are returned

#### Scenario: Non-member cannot view events
- **WHEN** a user who is not a member of team "acme" attempts to view an event
- **THEN** the request is rejected as unauthorized

---

### Requirement: Organizer can cancel an event
The system SHALL allow organizers to cancel an active event. When an event is
cancelled, the system SHALL publish a `TicketedEventCancelledDomainEvent` which
is mapped to a `TicketedEventCancelledModuleEvent` for cross-module notification.
Ticket type cancellation is no longer cascaded within the Organization module; the
Registrations module handles lifecycle status updates independently via the
event-lifecycle-sync capability.

#### Scenario: Cancel an active event
- **WHEN** an organizer cancels event "conf-2026" which is active
- **THEN** the event status is changed to cancelled and a `TicketedEventCancelledModuleEvent` is published

#### Scenario: Reject cancelling an already cancelled event
- **WHEN** an organizer attempts to cancel event "meetup-q1" which is already cancelled
- **THEN** the request is rejected because the event is already cancelled

---

### Requirement: Organizer can archive an event
The system SHALL allow organizers to archive an active or cancelled event. When an
event is archived, the system SHALL publish a `TicketedEventArchivedDomainEvent`
which is mapped to a `TicketedEventArchivedModuleEvent` for cross-module notification.

#### Scenario: Archive an active event
- **WHEN** an organizer archives event "conf-2025" which is active
- **THEN** the event status is changed to archived and a `TicketedEventArchivedModuleEvent` is published

#### Scenario: Archive a cancelled event
- **WHEN** an organizer archives event "meetup-q1" which is cancelled
- **THEN** the event status is changed to archived and a `TicketedEventArchivedModuleEvent` is published

#### Scenario: Reject archiving an already archived event
- **WHEN** an organizer attempts to archive event "conf-2024" which is already archived
- **THEN** the request is rejected because the event is already archived
