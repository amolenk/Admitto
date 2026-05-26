## MODIFIED Requirements

### Requirement: TicketedEvent owns the reconfirm policy

The `TicketedEvent` aggregate SHALL own an optional
`TicketedEventReconfirmPolicy` value object storing:

- a reconfirmation `Window` with `OpensAt` and `ClosesAt` datetimes,
- a `Cadence` expressed as a positive duration (minimum 1 day) describing how often the scheduler ticks to evaluate reconfirmation,
- a `MinEmailInterval` expressed as a positive integer in hours (minimum 1) representing the minimum time that must elapse since the later of (an attendee's registration time, the last reconfirmation email sent to that attendee) before the system will send them another reconfirmation email,
- an `AutoCancelEnabled` boolean (default `false`) that controls whether registrations are automatically cancelled after exceeding `MaxReconfirmAttempts` unanswered reconfirm emails, and
- a `MaxReconfirmAttempts` positive integer (minimum 1, required when `AutoCancelEnabled=true`, null/absent when `AutoCancelEnabled=false`) representing the number of reconfirm emails sent without reconfirmation before the system auto-cancels the registration.

The close datetime SHALL be strictly after the open datetime. The close datetime
SHALL be strictly before the event's `StartsAt`. The cadence SHALL be strictly positive and at least 1 day. The `MinEmailInterval` SHALL be a positive integer of at least 1 hour. When `AutoCancelEnabled=true`, `MaxReconfirmAttempts` SHALL be a positive integer of at least 1. The policy describes *when and how often* attendees are asked to reconfirm; sending messages and auto-cancellation decisions are not part of this capability. The policy is optional; when absent the system SHALL NOT ask attendees to reconfirm. The policy MAY be cleared. Configuring or updating the policy SHALL be rejected when the `TicketedEvent` status is Archived.

#### Scenario: Configure a reconfirm policy

- **WHEN** an organizer sets the reconfirm window for active event "DevConf" to "2025-05-01T00:00Z" / "2025-05-25T00:00Z" with cadence 7 days and MinEmailInterval 24 hours
- **THEN** the `TicketedEventReconfirmPolicy` is saved with the provided window, cadence, and MinEmailInterval, and `AutoCancelEnabled=false`

#### Scenario: Configure a reconfirm policy with auto-cancel enabled

- **WHEN** an organizer sets the reconfirm policy for active event "DevConf" with `AutoCancelEnabled=true` and `MaxReconfirmAttempts=3`
- **THEN** the policy is saved with `AutoCancelEnabled=true` and `MaxReconfirmAttempts=3`

#### Scenario: Update a reconfirm policy

- **WHEN** event "DevConf" has a reconfirm policy with cadence 7 days and MinEmailInterval 24 hours and an organizer updates cadence to 3 days and MinEmailInterval to 48 hours
- **THEN** the policy is updated to cadence 3 days and MinEmailInterval 48 hours

#### Scenario: Enable auto-cancel on an existing policy

- **WHEN** event "DevConf" has an existing reconfirm policy with `AutoCancelEnabled=false` and an organizer enables auto-cancel with `MaxReconfirmAttempts=2`
- **THEN** the policy is updated with `AutoCancelEnabled=true` and `MaxReconfirmAttempts=2`

#### Scenario: Remove a reconfirm policy

- **WHEN** event "DevConf" has a reconfirm policy and an organizer removes it
- **THEN** the policy no longer exists for "DevConf"

#### Scenario: Rejected — close before open

- **WHEN** an organizer sets a reconfirm window where the close datetime is before or equal to the open datetime
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — reconfirm window closes after event starts

- **WHEN** an organizer sets a reconfirm window whose `ClosesAt` is on or after the event's `StartsAt`
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — cadence below minimum

- **WHEN** an organizer sets a reconfirm cadence below 1 day
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — MinEmailInterval below minimum

- **WHEN** an organizer sets a MinEmailInterval below 1 hour
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — MaxReconfirmAttempts required when auto-cancel is enabled

- **WHEN** an organizer enables `AutoCancelEnabled=true` but provides no `MaxReconfirmAttempts` (or sets it to null)
- **THEN** the request is rejected with a validation error indicating MaxReconfirmAttempts is required

#### Scenario: Rejected — MaxReconfirmAttempts below minimum

- **WHEN** an organizer sets `AutoCancelEnabled=true` and `MaxReconfirmAttempts=0`
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — event is Archived

- **WHEN** event "DevConf" has status Archived and an organizer attempts to configure the reconfirm policy
- **THEN** the `TicketedEvent` rejects the mutation with reason "event not active"
