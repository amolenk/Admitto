## MODIFIED Requirements

### Requirement: Event cancellation is synced to the Registrations module
When the Organization module cancels a ticketed event, the system SHALL publish a
`TicketedEventCancelledModuleEvent`. The Registrations module SHALL process this
event and set the lifecycle status to Cancelled on the event's
`TicketedEventLifecycleGuard`. If no guard exists for the event, the system SHALL
create one with lifecycle status Cancelled.

When the guard's status changes from Active to Cancelled the handler SHALL also
increment the guard's `PolicyMutationCount` in the same unit of work, per the
event-lifecycle-guard capability.

The Registrations module SHALL NOT auto-create a `RegistrationPolicy` in response
to lifecycle events.

#### Scenario: Event cancellation synced to existing guard
- **WHEN** event "conf-2026" has a guard with status Active and a `TicketedEventCancelledModuleEvent` is processed
- **THEN** the guard status is set to Cancelled and `PolicyMutationCount` is incremented

#### Scenario: Event cancellation creates guard if none exists
- **WHEN** event "conf-2026" has no guard and a `TicketedEventCancelledModuleEvent` is processed
- **THEN** a guard is created for "conf-2026" with status Cancelled and `PolicyMutationCount = 1`

#### Scenario: Cancellation does not auto-create a registration policy
- **WHEN** event "conf-2026" has no `RegistrationPolicy` and a `TicketedEventCancelledModuleEvent` is processed
- **THEN** no `RegistrationPolicy` is created

---

### Requirement: Event archival is synced to the Registrations module
When the Organization module archives a ticketed event, the system SHALL publish a
`TicketedEventArchivedModuleEvent`. The Registrations module SHALL process this
event and set the lifecycle status to Archived on the event's
`TicketedEventLifecycleGuard`. If no guard exists for the event, the system SHALL
create one with lifecycle status Archived.

When the guard's status changes to Archived the handler SHALL also increment the
guard's `PolicyMutationCount` in the same unit of work.

The Registrations module SHALL NOT auto-create a `RegistrationPolicy` in response
to lifecycle events.

#### Scenario: Event archival synced to existing guard (Active)
- **WHEN** event "conf-2025" has a guard with status Active and a `TicketedEventArchivedModuleEvent` is processed
- **THEN** the guard status is set to Archived and `PolicyMutationCount` is incremented

#### Scenario: Event archival synced to existing guard (Cancelled)
- **WHEN** event "conf-2025" has a guard with status Cancelled and a `TicketedEventArchivedModuleEvent` is processed
- **THEN** the guard status is set to Archived and `PolicyMutationCount` is incremented

#### Scenario: Event archival creates guard if none exists
- **WHEN** event "conf-2025" has no guard and a `TicketedEventArchivedModuleEvent` is processed
- **THEN** a guard is created for "conf-2025" with status Archived and `PolicyMutationCount = 1`

---

### Requirement: Lifecycle sync is idempotent
The system SHALL handle duplicate lifecycle events gracefully. Processing a
cancellation event for an already-cancelled guard or an archival event for an
already-archived guard SHALL succeed without error. Idempotent applications
SHALL NOT increment `PolicyMutationCount`.

#### Scenario: Duplicate cancellation event is a no-op
- **WHEN** a `TicketedEventCancelledModuleEvent` arrives for event "conf-2026" whose guard status is already Cancelled and `PolicyMutationCount = 6`
- **THEN** the event is processed successfully, the status remains Cancelled, and `PolicyMutationCount` remains 6

#### Scenario: Duplicate archival event is a no-op
- **WHEN** a `TicketedEventArchivedModuleEvent` arrives for event "conf-2025" whose guard status is already Archived
- **THEN** the event is processed successfully with no state change
