# Event Lifecycle Sync Specification

### Requirement: Event cancellation is synced to the Registrations module
When the Organization module cancels a ticketed event, the system SHALL publish a
`TicketedEventCancelledModuleEvent`. The Registrations module SHALL process this
event and set the event lifecycle status to Cancelled on the event's registration
policy. If no registration policy exists for the event, the system SHALL create one
with lifecycle status Cancelled.

#### Scenario: Event cancellation synced to existing policy
- **WHEN** event "conf-2026" has a registration policy with lifecycle status Active and a `TicketedEventCancelledModuleEvent` is processed
- **THEN** the registration policy lifecycle status is set to Cancelled

#### Scenario: Event cancellation creates policy if none exists
- **WHEN** event "conf-2026" has no registration policy and a `TicketedEventCancelledModuleEvent` is processed
- **THEN** a registration policy is created for "conf-2026" with lifecycle status Cancelled

---

### Requirement: Event archival is synced to the Registrations module
When the Organization module archives a ticketed event, the system SHALL publish a
`TicketedEventArchivedModuleEvent`. The Registrations module SHALL process this
event and set the event lifecycle status to Archived on the event's registration
policy. If no registration policy exists for the event, the system SHALL create one
with lifecycle status Archived.

#### Scenario: Event archival synced to existing policy
- **WHEN** event "conf-2025" has a registration policy with lifecycle status Active and a `TicketedEventArchivedModuleEvent` is processed
- **THEN** the registration policy lifecycle status is set to Archived

#### Scenario: Event archival for cancelled event
- **WHEN** event "conf-2025" has a registration policy with lifecycle status Cancelled and a `TicketedEventArchivedModuleEvent` is processed
- **THEN** the registration policy lifecycle status is set to Archived

#### Scenario: Event archival creates policy if none exists
- **WHEN** event "conf-2025" has no registration policy and a `TicketedEventArchivedModuleEvent` is processed
- **THEN** a registration policy is created for "conf-2025" with lifecycle status Archived

---

### Requirement: Lifecycle sync is idempotent
The system SHALL handle duplicate lifecycle events gracefully. Processing a
cancellation event for an already-cancelled policy or an archival event for an
already-archived policy SHALL succeed without error.

#### Scenario: Duplicate cancellation event
- **WHEN** a `TicketedEventCancelledModuleEvent` arrives for event "conf-2026" whose policy lifecycle status is already Cancelled
- **THEN** the event is processed successfully with no change

#### Scenario: Duplicate archival event
- **WHEN** a `TicketedEventArchivedModuleEvent` arrives for event "conf-2025" whose policy lifecycle status is already Archived
- **THEN** the event is processed successfully with no change
