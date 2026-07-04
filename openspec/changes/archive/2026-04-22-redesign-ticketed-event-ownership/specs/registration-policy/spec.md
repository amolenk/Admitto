## REMOVED Requirements

### Requirement: Organizer can configure a registration window
**Reason**: Consolidated onto the `TicketedEvent` aggregate as the `TicketedEventRegistrationPolicy` value object.
**Migration**: See `event-management` → "TicketedEvent owns the registration policy". Behaviour is preserved: window stored, close-after-open validated, mutations rejected when event is Cancelled or Archived.

---

### Requirement: Organizer can configure an email domain restriction
**Reason**: Same — consolidated onto `TicketedEventRegistrationPolicy`.
**Migration**: See `event-management` → "TicketedEvent owns the registration policy". The email-domain restriction is part of the same value object; the scenarios in the new requirement cover configure / remove / enforce-on-attendee-registration behaviour.

---

### Requirement: Registration openness is derived from window and lifecycle
**Reason**: Moved to `event-management` so "openness" derivation sits next to the event aggregate that owns both inputs.
**Migration**: See `event-management` → "Registration openness is derived from window and event status". The derivation reads the `TicketedEventRegistrationPolicy` window and `TicketedEvent.Status` instead of the removed lifecycle-guard status.
