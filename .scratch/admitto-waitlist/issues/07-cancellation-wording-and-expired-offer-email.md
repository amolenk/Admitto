# 07: Cancellation wording + waitlist-offer-expired email

**What to build:** Two accuracy fixes to attendee-facing emails around negative/terminal outcomes: cancelling a purely-waitlisted registration no longer implies a ticket was ever held, and an attendee whose promotion offer lapses unclaimed is told so, instead of silently losing their spot.

**Blocked by:** 01 (Waitlisted registration status)

**Status:** ready-for-agent

- [ ] Cancelling a purely `Waitlisted` registration sends cancellation email wording distinct from today's wording (which implies a confirmed ticket existed), describing removal from a waitlist instead.
- [ ] Cancelling a `Registered` attendee (with or without additional waitlist entries) continues to use today's existing cancellation wording, unchanged.
- [ ] When an issued waitlist coupon expires unclaimed and the queue advances to the next entry, a new transactional email is sent to the attendee whose coupon expired, informing them their offer lapsed.
- [ ] This new expired-offer email's content is distinct from the existing waitlist-offer (promotion) email.
- [ ] Integration tests cover: cancelling a `Waitlisted` registration produces the new wording; cancelling a `Registered` one is unchanged; the expired-coupon job path produces exactly one expired-offer email per expired coupon.
