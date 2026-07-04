## ADDED Requirements

### Requirement: Organizer can configure a reconfirmation window and cadence

The system SHALL allow organizers (Owner or Organizer role) to configure a `ReconfirmPolicy` for an event that stores:

- a reconfirmation `Window` with `OpensAt` and `ClosesAt` datetimes, and
- a `Cadence` expressed as a positive duration (minimum 1 day) describing how often attendees are asked to reconfirm.

The close datetime SHALL be strictly after the open datetime. The cadence SHALL be strictly positive. The policy describes *when and how often* attendees should be asked to reconfirm; sending reconfirmation messages is not part of this capability.

The reconfirm policy is optional. When no `ReconfirmPolicy` is configured for an event, the system SHALL NOT ask attendees to reconfirm.

Configuring or updating the reconfirm policy SHALL go through the lifecycle guard (see event-lifecycle-guard) and SHALL therefore only succeed when the event's lifecycle status is Active.

#### Scenario: Configure a reconfirmation policy
- **WHEN** an organizer sets the reconfirmation window for event "DevConf" to "2025-05-01T00:00Z" / "2025-05-25T00:00Z" with cadence 7 days
- **THEN** the reconfirm policy is saved with the provided window and cadence

#### Scenario: Update a reconfirmation policy
- **WHEN** event "DevConf" has a reconfirm policy with cadence 7 days and an organizer updates it to cadence 3 days
- **THEN** the reconfirm policy cadence is updated to 3 days

#### Scenario: Remove a reconfirmation policy
- **WHEN** event "DevConf" has a reconfirm policy and an organizer removes it
- **THEN** the reconfirm policy no longer exists for "DevConf"

#### Scenario: Rejected — close before open
- **WHEN** an organizer sets a reconfirmation window where the close datetime is before or equal to the open datetime
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — non-positive cadence
- **WHEN** an organizer sets a reconfirmation cadence of zero or a negative duration
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — cadence below minimum
- **WHEN** an organizer sets a reconfirmation cadence below the minimum of 1 day
- **THEN** the request is rejected with a validation error

#### Scenario: Reject configuring on a Cancelled event
- **WHEN** event "DevConf" has a lifecycle guard with status Cancelled and an organizer attempts to configure the reconfirm policy
- **THEN** the request is rejected with reason "event not active"

#### Scenario: Reject configuring on an Archived event
- **WHEN** event "DevConf" has a lifecycle guard with status Archived and an organizer attempts to configure the reconfirm policy
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Absent policy means never reconfirm

When no `ReconfirmPolicy` exists for an event, the system SHALL treat the event as not requiring reconfirmation. There is no implicit default cadence or window.

#### Scenario: No policy means no reconfirmation
- **WHEN** event "DevConf" has no reconfirm policy configured
- **THEN** the system does not ask any attendee of "DevConf" to reconfirm

---

### Requirement: Team members can read the reconfirm policy

The system SHALL allow team members with Crew role or above to view the event's reconfirm policy (including its absence). The response SHALL include the configured window and cadence or indicate that no policy is configured.

#### Scenario: Read a configured reconfirm policy
- **WHEN** a Crew member views the reconfirm policy for event "DevConf" which has window "2025-05-01T00:00Z" / "2025-05-25T00:00Z" and cadence 7 days
- **THEN** the response includes the window and cadence

#### Scenario: Read an unconfigured reconfirm policy
- **WHEN** a Crew member views the reconfirm policy for event "DevConf" which has no policy configured
- **THEN** the response indicates no policy is configured
