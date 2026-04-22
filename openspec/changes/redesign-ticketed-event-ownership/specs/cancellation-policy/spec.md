## REMOVED Requirements

### Requirement: Organizer can configure a late-cancellation cutoff
**Reason**: Consolidated onto the `TicketedEvent` aggregate as the `TicketedEventCancellationPolicy` value object.
**Migration**: See `event-management` → "TicketedEvent owns the cancellation policy". Configure, update, and remove behaviour is preserved; mutation is rejected when event is Cancelled or Archived.

---

### Requirement: Cancellation classification is derivable from the policy
**Reason**: Classification logic is unchanged but now lives on the `TicketedEvent` aggregate.
**Migration**: See `event-management` → "TicketedEvent owns the cancellation policy". Late/on-time classification scenarios (before / at / after cutoff, and no-policy case) are preserved in the new requirement.

---

### Requirement: Team members can read the cancellation policy
**Reason**: Read access moves with the aggregate into Registrations and is served alongside other event details.
**Migration**: The policy is part of the `TicketedEvent` detail read path (see `event-management` → "Team member can view event details"). The response shape includes the `TicketedEventCancellationPolicy` or signals its absence.
