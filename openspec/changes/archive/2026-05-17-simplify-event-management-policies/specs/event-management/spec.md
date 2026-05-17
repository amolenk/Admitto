## REMOVED Requirements

### Requirement: TicketedEvent owns the cancellation policy
**Reason**: The late-cancellation classification was pure metadata with no downstream business logic acting on it. A hard-coded guard (reject self-service cancellation once the event has started) replaces any practical need for a configurable policy. Removing this policy simplifies the domain model and eliminates a UI page.
**Migration**: Remove all `TicketedEventCancellationPolicy` data and the corresponding API endpoints (`PUT` / `DELETE …/cancellation-policy`). Clients that queried the cancellation policy endpoint should be updated to no longer use it.

---

### Requirement: Organizer can cancel an event
**Reason**: The `Cancelled` lifecycle status is being removed (see design). Organizers use bulk email to inform attendees, then archive the event directly. The cancel step added complexity without operational value.
**Migration**: Remove the `POST …/cancel` endpoint. Any existing events with `Cancelled` status are migrated to `Archived`. Clients that checked for `"cancelled"` status must be updated to only expect `"active"` or `"archived"`.

---

## MODIFIED Requirements

### Requirement: TicketedEvent owns the reconfirm policy
The `TicketedEvent` aggregate SHALL own an optional
`TicketedEventReconfirmPolicy` value object storing:

- a reconfirmation `Window` with `OpensAt` and `ClosesAt` datetimes,
- a `Cadence` expressed as a positive duration (minimum 1 day) describing how often the scheduler ticks to evaluate reconfirmation, and
- a `MinEmailInterval` expressed as a positive integer in hours (minimum 1) representing the minimum time that must elapse since the later of (an attendee's registration time, the last reconfirmation email sent to that attendee) before the system will send them another reconfirmation email.

The close datetime SHALL be strictly after the open datetime. The cadence SHALL be strictly positive and at least 1 day. The `MinEmailInterval` SHALL be a positive integer of at least 1 hour. The policy describes *when and how often* attendees are asked to reconfirm; sending messages is not part of this capability. The policy is optional; when absent the system SHALL NOT ask attendees to reconfirm. The policy MAY be cleared. Configuring or updating the policy SHALL be rejected when the `TicketedEvent` status is Archived.

#### Scenario: Configure a reconfirm policy
- **WHEN** an organizer sets the reconfirm window for active event "DevConf" to "2025-05-01T00:00Z" / "2025-05-25T00:00Z" with cadence 7 days and MinEmailInterval 24 hours
- **THEN** the `TicketedEventReconfirmPolicy` is saved with the provided window, cadence, and MinEmailInterval

#### Scenario: Update a reconfirm policy
- **WHEN** event "DevConf" has a reconfirm policy with cadence 7 days and MinEmailInterval 24 hours and an organizer updates cadence to 3 days and MinEmailInterval to 48 hours
- **THEN** the policy is updated to cadence 3 days and MinEmailInterval 48 hours

#### Scenario: Remove a reconfirm policy
- **WHEN** event "DevConf" has a reconfirm policy and an organizer removes it
- **THEN** the policy no longer exists for "DevConf"

#### Scenario: Rejected — close before open
- **WHEN** an organizer sets a reconfirm window where the close datetime is before or equal to the open datetime
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — cadence below minimum
- **WHEN** an organizer sets a reconfirm cadence below 1 day
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — MinEmailInterval below minimum
- **WHEN** an organizer sets a MinEmailInterval below 1 hour
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — event is Archived
- **WHEN** event "DevConf" has status Archived and an organizer attempts to configure the reconfirm policy
- **THEN** the `TicketedEvent` rejects the mutation with reason "event not active"

---

### Requirement: Organizer can archive an event
The system SHALL allow organizers to archive an **active** `TicketedEvent`. The command is handled by the Registrations module. In the same unit of work the `TicketedEvent` aggregate SHALL transition its status to Archived, publish an in-module domain event that projects `EventStatus = Archived` onto the event's `TicketCatalog`, and outbox a `TicketedEventArchived` integration event to Organization.

Archiving from the `Cancelled` status is no longer supported because the `Cancelled` status has been removed.

#### Scenario: Archive an active event
- **WHEN** an organizer archives event "conf-2025" which is active
- **THEN** the `TicketedEvent` status is changed to Archived, the `TicketCatalog.EventStatus` is set to Archived, and a `TicketedEventArchived` integration event is outboxed

#### Scenario: Reject archiving an already archived event
- **WHEN** an organizer attempts to archive event "conf-2024" which is already archived
- **THEN** the request is rejected because the event is already archived

---

### Requirement: TicketedEvent lifecycle is Active and Archived only
The `TicketedEvent` aggregate SHALL support exactly two lifecycle statuses: `Active` and `Archived`. The `Cancelled` status is removed. Transition `Active → Archived` is the only permitted lifecycle mutation after creation. The `TicketedEvent` aggregate SHALL reject modifications to itself when its own status is Archived (replacing the prior Cancelled-or-Archived guard).

#### Scenario: Reject update of archived event
- **WHEN** an organizer attempts to update the name of an archived event
- **THEN** the `TicketedEvent` rejects the update with reason "event not active"

#### Scenario: List events returns only active events
- **WHEN** an admin calls `GET /admin/teams/{teamId}/events` and events with active and archived status exist
- **THEN** only active events are returned (archived events are excluded)
