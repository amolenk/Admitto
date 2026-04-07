## ADDED Requirements

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
