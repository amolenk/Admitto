# Cancellation Policy Specification

### Requirement: Organizer can configure a late-cancellation cutoff

The system SHALL allow organizers (Owner or Organizer role) to configure a `CancellationPolicy` for an event that stores a single `LateCancellationCutoff` datetime. Attendee-initiated cancellations submitted on or after that moment SHALL be classified as "late"; cancellations submitted before it SHALL be classified as "on time". The policy configuration itself does not reject cancellations, nor does it impose fees — it is pure classification data for downstream consumers.

The cancellation policy is optional. When no `CancellationPolicy` is configured for an event, no cancellation is ever classified as late.

Configuring or updating the cancellation policy SHALL go through the lifecycle guard (see event-lifecycle-guard) and SHALL therefore only succeed when the event's lifecycle status is Active.

#### Scenario: Configure a late-cancellation cutoff
- **WHEN** an organizer sets the late-cancellation cutoff for event "DevConf" to "2025-05-25T00:00Z"
- **THEN** the cancellation policy is saved for "DevConf" with `LateCancellationCutoff = 2025-05-25T00:00Z`

#### Scenario: Update a late-cancellation cutoff
- **WHEN** event "DevConf" has a cancellation policy with cutoff "2025-05-25T00:00Z" and an organizer updates it to "2025-05-20T00:00Z"
- **THEN** the cancellation policy cutoff is updated to "2025-05-20T00:00Z"

#### Scenario: Remove a cancellation policy
- **WHEN** event "DevConf" has a cancellation policy and an organizer removes it
- **THEN** the cancellation policy no longer exists for "DevConf" and no cancellation is classified as late

#### Scenario: Reject configuring on a Cancelled event
- **WHEN** event "DevConf" has a lifecycle guard with status Cancelled and an organizer attempts to set the late-cancellation cutoff
- **THEN** the request is rejected with reason "event not active"

#### Scenario: Reject configuring on an Archived event
- **WHEN** event "DevConf" has a lifecycle guard with status Archived and an organizer attempts to set the late-cancellation cutoff
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Cancellation classification is derivable from the policy

Given a cancellation instant and the event's `CancellationPolicy`, the system SHALL classify the cancellation as "late" if and only if a policy exists and the cancellation instant is greater than or equal to `LateCancellationCutoff`. Otherwise the cancellation is classified as "on time".

#### Scenario: Cancellation before cutoff is on time
- **WHEN** event "DevConf" has cutoff "2025-05-25T00:00Z" and an attendee cancels at "2025-05-20T12:00Z"
- **THEN** the cancellation is classified as on time

#### Scenario: Cancellation at cutoff is late
- **WHEN** event "DevConf" has cutoff "2025-05-25T00:00Z" and an attendee cancels at exactly "2025-05-25T00:00Z"
- **THEN** the cancellation is classified as late

#### Scenario: Cancellation after cutoff is late
- **WHEN** event "DevConf" has cutoff "2025-05-25T00:00Z" and an attendee cancels at "2025-05-28T00:00Z"
- **THEN** the cancellation is classified as late

#### Scenario: No policy means never late
- **WHEN** event "DevConf" has no cancellation policy and an attendee cancels at "2025-07-01T00:00Z"
- **THEN** the cancellation is classified as on time

---

### Requirement: Team members can read the cancellation policy

The system SHALL allow team members with Crew role or above to view the event's cancellation policy (including its absence). The response SHALL include the configured `LateCancellationCutoff` or indicate that no policy is configured.

#### Scenario: Read a configured cancellation policy
- **WHEN** a Crew member of team "acme" views the cancellation policy for event "DevConf" which has cutoff "2025-05-25T00:00Z"
- **THEN** the response indicates a policy exists with cutoff "2025-05-25T00:00Z"

#### Scenario: Read an unconfigured cancellation policy
- **WHEN** a Crew member views the cancellation policy for event "DevConf" which has no policy configured
- **THEN** the response indicates no policy is configured
