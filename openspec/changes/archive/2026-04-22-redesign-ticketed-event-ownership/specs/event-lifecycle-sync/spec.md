## REMOVED Requirements

### Requirement: Event cancellation is synced to the Registrations module
**Reason**: The `TicketedEvent` aggregate now lives in the Registrations module and performs its own lifecycle transition. There is no longer a cross-module sync from Organization → Registrations; lifecycle integration events flow the opposite direction.
**Migration**: Cancellation now originates in Registrations. The new `TicketedEventCancelled` integration event (Registrations → Organization) is specified in `event-management` under "Lifecycle transitions publish integration events to Organization". `TicketedEventLifecycleGuard` is removed; its status responsibility is now a single `EventStatus` field on `TicketCatalog`, projected in the same unit of work as the `TicketedEvent` status change.

---

### Requirement: Event archival is synced to the Registrations module
**Reason**: Same as above — archive now originates in Registrations.
**Migration**: See the new `TicketedEventArchived` integration event in `event-management`.

---

### Requirement: Lifecycle sync is idempotent
**Reason**: Idempotency moves to the consumer side (Organization handles redelivery of `TicketedEvent*` integration events). The new invariant lives in `team-management` under "Team counters react to Registrations integration events" and "Redelivered creation-success is idempotent".
**Migration**: Organization's integration-event handlers use the `CreationRequestId` (for creation responses) or `TicketedEventId` (for lifecycle events) together with the target entity/counter state as the idempotency guard.
