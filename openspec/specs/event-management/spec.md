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
event's details and ticket types by event slug.

#### Scenario: View event details with ticket types
- **WHEN** a Crew member of team "acme" views event "conf-2026" which has two ticket types
- **THEN** the event's name, dates, URLs, status, and both ticket types are returned

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
cancelled, the system SHALL cancel all its active ticket types.

#### Scenario: Cancel an active event
- **WHEN** an organizer cancels event "conf-2026" which is active and has two active ticket types
- **THEN** the event status is changed to cancelled and both ticket types are cancelled

#### Scenario: Reject cancelling an already cancelled event
- **WHEN** an organizer attempts to cancel event "meetup-q1" which is already cancelled
- **THEN** the request is rejected because the event is already cancelled

---

### Requirement: Organizer can archive an event
The system SHALL allow organizers to archive an active or cancelled event.

#### Scenario: Archive an active event
- **WHEN** an organizer archives event "conf-2025" which is active
- **THEN** the event status is changed to archived

#### Scenario: Archive a cancelled event
- **WHEN** an organizer archives event "meetup-q1" which is cancelled
- **THEN** the event status is changed to archived

#### Scenario: Reject archiving an already archived event
- **WHEN** an organizer attempts to archive event "conf-2024" which is already archived
- **THEN** the request is rejected because the event is already archived

---

### Requirement: Organizer can add ticket types to an event
The system SHALL allow organizers to add a ticket type to an active event with a
slug, name, self-service flag, time slots, and optional capacity. Ticket type
slugs SHALL be unique within an event. The system SHALL reject adding ticket types
to cancelled or archived events.

#### Scenario: Add a ticket type to an active event
- **WHEN** an organizer adds a ticket type with slug "vip", name "VIP Pass", self-service enabled, time slots ["morning", "afternoon"], and capacity 100 to event "conf-2026"
- **THEN** the event has a ticket type "vip" with the provided details

#### Scenario: Reject duplicate ticket type slug
- **WHEN** event "conf-2026" already has a ticket type with slug "vip" and an organizer adds another with slug "vip"
- **THEN** the request is rejected with a duplicate ticket type slug error

#### Scenario: Reject adding ticket type to cancelled event
- **WHEN** an organizer attempts to add a ticket type to a cancelled event
- **THEN** the request is rejected because the event is cancelled

---

### Requirement: Organizer can update ticket types
The system SHALL allow organizers to update a ticket type's name, capacity, and
self-service availability. Ticket type slugs SHALL be immutable after creation.

#### Scenario: Update a ticket type's capacity and availability
- **WHEN** an organizer updates ticket type "vip" to capacity 200 and disables self-service availability
- **THEN** the ticket type capacity is changed to 200 and self-service availability is disabled

#### Scenario: Reject changing a ticket type's slug
- **WHEN** an organizer attempts to change the slug of ticket type "vip" to "premium"
- **THEN** the request is rejected because ticket type slugs are immutable

---

### Requirement: Organizer can cancel ticket types
The system SHALL allow organizers to cancel a ticket type, preventing new
registrations for it.

#### Scenario: Cancel a ticket type
- **WHEN** an organizer cancels active ticket type "vip" on event "conf-2026"
- **THEN** the ticket type is marked as cancelled and no new registrations can be made for it

#### Scenario: Reject cancelling an already cancelled ticket type
- **WHEN** an organizer attempts to cancel ticket type "early-bird" which is already cancelled
- **THEN** the request is rejected because the ticket type is already cancelled
