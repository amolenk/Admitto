## ADDED Requirements

### Requirement: Registration policy tracks event lifecycle status
The system SHALL track the event lifecycle status (Active, Cancelled, Archived) as
part of the event's registration policy. The status SHALL default to Active when a
policy is created. The status SHALL be updated by lifecycle events from the
Organization module (see event-lifecycle-sync capability). When the lifecycle status
is Cancelled or Archived, all registration flows SHALL be blocked and ticket type
modifications SHALL be rejected.

#### Scenario: Default lifecycle status is Active
- **WHEN** a registration policy is created for event "conf-2026" and no lifecycle event has been received
- **THEN** the lifecycle status is Active

#### Scenario: Registrations blocked when lifecycle status is Cancelled
- **WHEN** the event lifecycle status for "conf-2026" is Cancelled and an attendee attempts to register
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Registrations blocked when lifecycle status is Archived
- **WHEN** the event lifecycle status for "conf-2026" is Archived and an attendee attempts to register
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Ticket type modifications blocked when lifecycle status is Cancelled
- **WHEN** the event lifecycle status for "conf-2026" is Cancelled and an organizer attempts to add a ticket type
- **THEN** the request is rejected because the event is cancelled
