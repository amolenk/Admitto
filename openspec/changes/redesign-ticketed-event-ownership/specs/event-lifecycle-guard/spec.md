## REMOVED Requirements

### Requirement: Lifecycle guard aggregate exists per event
**Reason**: The `TicketedEventLifecycleGuard` aggregate is deleted. The `TicketedEvent` aggregate (now in Registrations) is the source of truth for lifecycle status. The separate guard is no longer needed because policy mutations happen on the `TicketedEvent` itself.
**Migration**: The guard's status field is replaced by `TicketedEvent.Status` (authoritative) and a read-only `TicketCatalog.EventStatus` projection for the registration gate. Both are specified in `ticket-type-management` and `event-management`.

---

### Requirement: Policy mutations assert Active and bump the mutation count
**Reason**: Policies are consolidated as value objects on the `TicketedEvent` aggregate. Mutating a policy is a mutation of `TicketedEvent` itself — optimistic concurrency on the `TicketedEvent` row serves the role formerly played by `PolicyMutationCount`.
**Migration**: Each `TicketedEvent`-level policy configuration command rejects the mutation when `TicketedEvent.Status` is not Active. No separate mutation counter exists; `TicketedEvent.Version` handles concurrency. See `event-management`: "TicketedEvent owns the registration policy", "TicketedEvent owns the cancellation policy", "TicketedEvent owns the reconfirm policy".

---

### Requirement: Lifecycle transitions bump the mutation count
**Reason**: Not needed once policies and lifecycle live in the same aggregate. A cancel/archive on `TicketedEvent` naturally conflicts with concurrent policy edits via the aggregate's own row-version token.
**Migration**: Remove the code path; rely on `TicketedEvent.Version` for concurrency.

---

### Requirement: PolicyMutationCount is not exposed on any read API
**Reason**: The field itself is removed.
**Migration**: Nothing to migrate; no public or admin API was reading this value.
