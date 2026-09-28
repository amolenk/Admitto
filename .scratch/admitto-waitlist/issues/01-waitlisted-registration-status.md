# 01: Waitlisted registration status

**What to build:** A registration that holds zero confirmed tickets is a first-class, ongoing state (`Waitlisted`) rather than a signup that leaves no trace. Every self-service registration — including one that selects only waitlist-mode ticket types — produces a durable Registration record the attendee can look up later, and actions that only make sense for a confirmed attendee (checking in, reconfirming) are correctly refused for one who is only waitlisted.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `RegistrationStatus` gains a third value, `Waitlisted`, alongside the existing `Registered` and `Cancelled`.
- [ ] A self-service registration that selects only waitlist-mode ticket types creates a Registration with status `Waitlisted` (today it creates no Registration at all).
- [ ] A registration's status is recomputed on every ticket-composition change: zero confirmed tickets → `Waitlisted`; one or more confirmed tickets → `Registered`.
- [ ] `Cancelled` remains a separate terminal status, reachable from either `Registered` or `Waitlisted`.
- [ ] Check-in is rejected with a business-rule violation for a `Waitlisted` registration, using the same error pattern as the existing `Cancelled` guard.
- [ ] Reconfirmation is rejected with a business-rule violation for a `Waitlisted` registration, using the same error pattern as the existing `Cancelled` guard.
- [ ] Existing self-service create-registration tests covering waitlist-only submissions are updated to assert a `Waitlisted` registration is produced.
- [ ] Domain tests cover the status-derivation rule directly on the Registration aggregate, in isolation.
