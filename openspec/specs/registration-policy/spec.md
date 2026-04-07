# Registration Policy Specification

### Requirement: Organizer can configure a registration window
The system SHALL allow organizers (Owner or Organizer role) to configure a
registration window (open and close datetimes) for an event. This configuration
is stored in the Registrations module as part of the event's registration policy.
The close datetime SHALL be after the open datetime. Self-service registrations
outside the window SHALL be rejected; coupon-based registrations are unaffected
unless the coupon has `bypassRegistrationWindow` disabled.

#### Scenario: Configure registration window
- **WHEN** an organizer sets the registration window for event "DevConf" from "2025-01-01T00:00Z" to "2025-06-01T00:00Z"
- **THEN** the registration window is saved for "DevConf"

#### Scenario: Update existing registration window
- **WHEN** an organizer updates the registration window for event "DevConf" from "2025-01-01T00:00Z" / "2025-06-01T00:00Z" to "2025-02-01T00:00Z" / "2025-07-01T00:00Z"
- **THEN** the registration window is updated

#### Scenario: Rejected — close before open
- **WHEN** an organizer sets a registration window where the close datetime is before the open datetime
- **THEN** the request is rejected with a validation error

---

### Requirement: Organizer can configure an email domain restriction
The system SHALL allow organizers (Owner or Organizer role) to configure an
optional email domain restriction (single domain pattern, e.g. "@acme.com") for
an event. Self-service registrations from non-matching domains SHALL be rejected.
Coupon-based registrations SHALL bypass domain restrictions. The restriction MAY
be removed, after which any email domain is accepted for self-service registration.

#### Scenario: Configure email domain restriction
- **WHEN** an organizer sets the allowed email domain for event "CorpConf" to "@acme.com"
- **THEN** self-service registrations for "CorpConf" are restricted to "@acme.com" emails

#### Scenario: Remove email domain restriction
- **WHEN** an organizer removes the email domain restriction from event "CorpConf" which was restricted to "@acme.com"
- **THEN** self-service registrations for "CorpConf" accept any email domain

---

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
