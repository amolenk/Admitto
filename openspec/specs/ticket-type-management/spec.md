# Ticket Type Management Specification

### Requirement: Organizer can add a ticket type to an event
The system SHALL allow organizers (Owner or Organizer role) to add a ticket type to
an event with a slug, name, time slots, and optional capacity. Ticket type slugs
SHALL be unique within an event. Adding a ticket type SHALL go through the event's
`TicketedEventLifecycleGuard` (see event-lifecycle-guard): the command is rejected
when the guard's status is Cancelled or Archived, and succeeds only when Active,
incrementing the guard's `PolicyMutationCount` in the same unit of work. When a
ticket type is the first one added for an event, the system SHALL create the
ticket catalog for that event.

#### Scenario: Add a ticket type to an active event
- **WHEN** an organizer adds a ticket type with slug "vip", name "VIP Pass", time slots ["morning", "afternoon"], and capacity 100 to event "conf-2026" whose guard is Active
- **THEN** the event has a ticket type "vip" with the provided details and used capacity 0, and the guard's `PolicyMutationCount` is incremented

#### Scenario: Add a ticket type with no capacity
- **WHEN** an organizer adds a ticket type with slug "speaker", name "Speaker Pass", and no capacity to event "conf-2026" whose guard is Active
- **THEN** the event has a ticket type "speaker" with no capacity set

#### Scenario: Reject duplicate ticket type slug
- **WHEN** event "conf-2026" already has a ticket type with slug "vip" and an organizer adds another with slug "vip"
- **THEN** the request is rejected with a duplicate ticket type slug error

#### Scenario: Reject adding ticket type when guard is Cancelled
- **WHEN** event "conf-2026" has a lifecycle guard with status Cancelled and an organizer attempts to add a ticket type
- **THEN** the request is rejected with reason "event not active"

#### Scenario: Reject adding ticket type when guard is Archived
- **WHEN** event "conf-2026" has a lifecycle guard with status Archived and an organizer attempts to add a ticket type
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Organizer can update a ticket type
The system SHALL allow organizers to update a ticket type's name and capacity.
Ticket type slugs SHALL be immutable after creation. Updating a ticket type SHALL
go through the event's `TicketedEventLifecycleGuard` and is rejected when the
guard's status is not Active; successful updates SHALL increment
`PolicyMutationCount` in the same unit of work.

#### Scenario: Update a ticket type's capacity
- **WHEN** an organizer updates ticket type "vip" to capacity 200 on an event whose guard is Active
- **THEN** the ticket type capacity is changed to 200 and the guard's `PolicyMutationCount` is incremented

#### Scenario: Update a ticket type's name
- **WHEN** an organizer updates ticket type "vip" name to "VIP Access" on an event whose guard is Active
- **THEN** the ticket type name is updated

#### Scenario: Reject update when guard is Cancelled
- **WHEN** the lifecycle guard status is Cancelled and an organizer attempts to update a ticket type
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Organizer can cancel a ticket type
The system SHALL allow organizers to cancel an active ticket type, preventing new
registrations for it. The system SHALL reject cancelling an already cancelled ticket
type. Cancelling a ticket type SHALL go through the event's
`TicketedEventLifecycleGuard` and is rejected when the guard's status is not Active;
successful cancellations SHALL increment `PolicyMutationCount` in the same unit of
work.

#### Scenario: Cancel a ticket type
- **WHEN** an organizer cancels active ticket type "vip" on event "conf-2026" whose guard is Active
- **THEN** the ticket type is marked as cancelled, no new registrations can be made for it, and the guard's `PolicyMutationCount` is incremented

#### Scenario: Reject cancelling an already cancelled ticket type
- **WHEN** an organizer attempts to cancel ticket type "early-bird" which is already cancelled
- **THEN** the request is rejected because the ticket type is already cancelled

#### Scenario: Reject cancelling ticket type when guard is Cancelled
- **WHEN** the lifecycle guard status is Cancelled and an organizer attempts to cancel a ticket type
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
