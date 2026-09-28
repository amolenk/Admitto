# 03: Cancellation withdraws waitlist entries

**What to build:** Cancelling a registration also takes that attendee off any waitlist they're still queued on, so a cancelled attendee can never receive a promotion offer afterward.

**Blocked by:** 01 (Waitlisted registration status)

**Status:** ready-for-agent

- [ ] Cancelling a registration removes every active waitlist entry belonging to that registration's email, across all ticket types for the event.
- [ ] Removing an entry as part of cancellation renumbers the remaining active entries' queue positions correctly.
- [ ] Cancelling a registration with no associated waitlist entries behaves exactly as before (no-op on the waitlist side).
- [ ] Both a purely `Waitlisted` registration and a mixed (`Registered` with separate waitlist entries) registration are covered by this behavior.
- [ ] An integration test verifies a cancelled attendee cannot subsequently be issued a promotion coupon, because their entry no longer exists.
