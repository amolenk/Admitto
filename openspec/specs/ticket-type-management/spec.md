# Ticket Type Management Specification

### Requirement: Organizer can add a ticket type to an event
The system SHALL allow organizers (Owner or Organizer role) to add a ticket type to
an event with a slug, name, time slots, and optional capacity. Ticket type slugs
SHALL be unique within an event. The system SHALL reject adding ticket types when
the event lifecycle status is Cancelled or Archived. When a ticket type is the first
one added for an event, the system SHALL create the ticket catalog for that event.

#### Scenario: Add a ticket type to an active event
- **WHEN** an organizer adds a ticket type with slug "vip", name "VIP Pass", time slots ["morning", "afternoon"], and capacity 100 to event "conf-2026"
- **THEN** the event has a ticket type "vip" with the provided details and used capacity 0

#### Scenario: Add a ticket type with no capacity
- **WHEN** an organizer adds a ticket type with slug "speaker", name "Speaker Pass", and no capacity to event "conf-2026"
- **THEN** the event has a ticket type "speaker" with no capacity set

#### Scenario: Reject duplicate ticket type slug
- **WHEN** event "conf-2026" already has a ticket type with slug "vip" and an organizer adds another with slug "vip"
- **THEN** the request is rejected with a duplicate ticket type slug error

#### Scenario: Reject adding ticket type to cancelled event
- **WHEN** the event lifecycle status is Cancelled and an organizer attempts to add a ticket type
- **THEN** the request is rejected because the event is cancelled

#### Scenario: Reject adding ticket type to archived event
- **WHEN** the event lifecycle status is Archived and an organizer attempts to add a ticket type
- **THEN** the request is rejected because the event is archived

---

### Requirement: Organizer can update a ticket type
The system SHALL allow organizers to update a ticket type's name and capacity.
Ticket type slugs SHALL be immutable after creation. The system SHALL reject updates
when the event lifecycle status is Cancelled or Archived.

#### Scenario: Update a ticket type's capacity
- **WHEN** an organizer updates ticket type "vip" to capacity 200
- **THEN** the ticket type capacity is changed to 200

#### Scenario: Update a ticket type's name
- **WHEN** an organizer updates ticket type "vip" name to "VIP Access"
- **THEN** the ticket type name is updated

#### Scenario: Reject update on cancelled event
- **WHEN** the event lifecycle status is Cancelled and an organizer attempts to update a ticket type
- **THEN** the request is rejected because the event is cancelled

---

### Requirement: Organizer can cancel a ticket type
The system SHALL allow organizers to cancel an active ticket type, preventing new
registrations for it. The system SHALL reject cancelling an already cancelled ticket
type. The system SHALL reject cancelling ticket types when the event lifecycle status
is Cancelled or Archived.

#### Scenario: Cancel a ticket type
- **WHEN** an organizer cancels active ticket type "vip" on event "conf-2026"
- **THEN** the ticket type is marked as cancelled and no new registrations can be made for it

#### Scenario: Reject cancelling an already cancelled ticket type
- **WHEN** an organizer attempts to cancel ticket type "early-bird" which is already cancelled
- **THEN** the request is rejected because the ticket type is already cancelled

#### Scenario: Reject cancelling ticket type on cancelled event
- **WHEN** the event lifecycle status is Cancelled and an organizer attempts to cancel a ticket type
- **THEN** the request is rejected because the event is cancelled

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
