## Context

Two Partner API changes support dynamic event sites. Both live in the Registrations module, which already owns `TicketedEvent`, the `AllowedEmailDomain` registration policy, and the `AdditionalDetailSchema`.

## Decisions

### OTP domain enforcement is unconditional
The OTP flow issues verification tokens used for new registration *and* for self-service management (cancel, reconfirm, update, resend). Enforcing the domain rule at request time therefore also blocks an existing attendee whose address no longer matches the domain. This is accepted: the domain restriction is intended to gate who may interact with the event, and honoring stale registrations would require an extra registration lookup and a carve-out that weakens the guard. The rule is a no-op for events without a configured domain.

The guard runs before the rate-limit check so rejected addresses do not consume an attendee's OTP budget. It reuses the existing `TicketedEvent.EnsureEmailDomainAllowed` aggregate method and its `registration.email_domain_not_allowed` (`Validation` → HTTP 400) error; no new domain concept is introduced.

The enumeration-safety requirement (return 202 regardless of whether a registration exists) is unchanged: the domain rule keys off public, per-event configuration, not on whether the address is registered, so it leaks no attendee data.

### New slice instead of reusing the admin detail query
The admin `GetTicketedEventDetails` DTO exposes internal id, team id, version, lifecycle status, and full reconfirm/waitlist policy objects, and its endpoint is team-membership scoped. Reusing it on the Partner API would over-expose internal state and pin one DTO to two audiences with divergent needs. A dedicated `GetPartnerTicketedEventDetails` slice keeps the partner contract minimal and lets the partner query evolve independently.

Naming follows the established partner convention (`GetPartnerRegistrationDetails`, `ResolvePartnerTicketedEvent`, `UpdatePartnerRegistration`).

### Route and lifecycle behavior
The endpoint maps `"/"` on the existing `/events/{eventSlug}` partner group → `GET /api/events/{eventSlug}`, mirroring how the admin endpoint maps `"/"` on its group. Slug resolution via `PartnerTicketedEventResolver` returns archived events too (consistent with `GetPublicTicketTypes`); the reduced payload carries no lifecycle status, and `isRegistrationOpen` is already false for non-active events.

### Ticket types stay separate
`GetPublicTicketTypes` continues to serve availability, which changes far more often than static event metadata and benefits from its own polling cadence. The new endpoint deliberately does not absorb it.

## Risks / Trade-offs

- Existing attendees under a now-disallowed domain lose self-service access (accepted, above).
- The mixed-case email-hash fix changes rate-limit/supersede behavior for previously-issued mixed-case codes; impact is limited to unexpired codes at deploy time.
