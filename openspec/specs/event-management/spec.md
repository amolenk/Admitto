## Purpose

Organizers create and manage ticketed events within their teams, including defining ticket types with capacities and time slots. Events can be updated, cancelled, and archived.

## Requirements

### Requirement: Organizer can create a ticketed event
The system SHALL allow organizers to create a ticketed event with a slug, name,
website URL, base URL, and start/end dates. Event slugs SHALL be unique within a
team. The end date SHALL be on or after the start date. The system SHALL reject
creating events for archived teams.

#### Scenario: Successfully create a ticketed event
- **WHEN** an organizer of team "acme" creates an event with slug "conf-2026", name "Acme Conf 2026", website "https://conf.acme.org", base URL "https://tickets.acme.org", starting 2026-06-01 and ending 2026-06-03
- **THEN** the event is created with the provided details and is in an active state with no ticket types

#### Scenario: Reject duplicate event slug within a team
- **WHEN** team "acme" already has an event with slug "conf-2026" and an organizer creates another event with slug "conf-2026"
- **THEN** the request is rejected with a duplicate slug error

#### Scenario: Reject end date before start date
- **WHEN** an organizer creates an event starting 2026-06-03 and ending 2026-06-01
- **THEN** the request is rejected with a validation error

#### Scenario: Reject creating an event for an archived team
- **WHEN** a team is archived and an organizer attempts to create an event for it
- **THEN** the request is rejected because the team is archived

#### Scenario: Crew member cannot create events
- **WHEN** a Crew member of team "acme" attempts to create an event
- **THEN** the request is rejected as unauthorized

---

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

### Requirement: Team member can list team events
The system SHALL allow team members with Crew role or above to list all events for
their team. Archived events SHALL be excluded from listings by default.

#### Scenario: List active events excludes archived
- **WHEN** a Crew member of team "acme" lists events and "conf-2026" (active), "meetup-q1" (cancelled), and "conf-2025" (archived) exist
- **THEN** "conf-2026" and "meetup-q1" are returned and "conf-2025" is not included

---

### Requirement: Organizer can update event details
The system SHALL allow organizers to update an event's name, website URL, base URL,
and start/end dates. The system SHALL use optimistic concurrency (expected version)
to prevent lost updates. The system SHALL prevent modifications to cancelled or
archived events.

#### Scenario: Update event details
- **WHEN** an organizer of team "acme" updates event "conf-2026" name to "Acme Conference 2026" with expected version 1 and the current version is 1
- **THEN** the event name is changed and the version is incremented

#### Scenario: Concurrent update conflict
- **WHEN** an organizer updates event "conf-2026" with expected version 1 but the current version is 2
- **THEN** the request is rejected with a concurrency conflict error

#### Scenario: Reject update of cancelled event
- **WHEN** an organizer attempts to update the name of a cancelled event
- **THEN** the request is rejected because the event is cancelled

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
