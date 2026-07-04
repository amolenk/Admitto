## ADDED Requirements

### Requirement: Self-service ticket change rejects ticket types not enabled for self-service
The system SHALL reject a self-service ticket change that would add a ticket type
with `SelfServiceEnabled = false` to the registration. The check applies only to
ticket types being newly claimed (i.e. in `toClaim`, not `toRelease`). Admin ticket
changes are not subject to this check.

#### Scenario: Self-service change rejected — new ticket type not self-service enabled
- **GIVEN** a registration holding ["General Admission"] on event "conf-2026", and "vip" has `SelfServiceEnabled = false`
- **WHEN** the attendee submits a self-service change to ["vip"]
- **THEN** the response is HTTP 422 with reason "ticket type not available for self-service"

#### Scenario: Self-service change allowed when all new ticket types are self-service enabled
- **GIVEN** a registration holding ["General Admission"] on event "conf-2026", and "workshop" has `SelfServiceEnabled = true`
- **WHEN** the attendee submits a self-service change to ["workshop"]
- **THEN** the change succeeds (assuming capacity and window are valid)
