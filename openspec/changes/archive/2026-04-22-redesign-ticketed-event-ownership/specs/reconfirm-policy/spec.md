## REMOVED Requirements

### Requirement: Organizer can configure a reconfirm window
**Reason**: Consolidated onto the `TicketedEvent` aggregate as the `TicketedEventReconfirmPolicy` value object.
**Migration**: See `event-management` → "TicketedEvent owns the reconfirm policy". Configure, update, remove, and validation (open-before-close, not-before-registration-close) are preserved; mutation is rejected when event is Cancelled or Archived.

---

### Requirement: Reconfirm openness is derivable from the policy
**Reason**: Derivation lives on the `TicketedEvent` aggregate now.
**Migration**: See `event-management` → "TicketedEvent owns the reconfirm policy". The "open when inside window and status is Active" derivation is preserved.

---

### Requirement: Team members can read the reconfirm policy
**Reason**: Read access moves into Registrations and is served with the rest of the event detail.
**Migration**: The policy is returned in the `TicketedEvent` detail read path (see `event-management` → "Team member can view event details").
