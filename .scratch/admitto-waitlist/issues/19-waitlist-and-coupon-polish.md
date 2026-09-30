# 19: Waitlist and coupon polish from the pre-release check

**What to build:** Clears the remaining should-fix items and nits from the pre-release sanity check (2026-09-29). None blocks the main flows once ticket 18 lands, but together they make the waitlist and coupons safe to operate: clearer emails, a safer admin UI, a clean partner contract, tests for critical paths and accurate docs.

**Blocked by:** 18 (Waitlisted registrations work in every registration flow) and 20 (Public capacity is the only enforced limit). The email wording and several tests describe behaviour that ticket 18 settles; the VIP, coupon and glossary items use ticket 20's capacity model.

**Status:** ready-for-agent

Each item is small and independent, so it can be split further if one grows.

### Emails

- [ ] The waitlist offer email shows the expiry in the event's time zone, with a zone label, in a culture-independent format. The time zone is already on the transactional email context; today the email uses server culture and UTC with no label.
- [ ] The offer email wording fits every way an offer is issued: automatic promotion, VIP promotion, and promote-all when the capacity limit is removed. "You've reached the top of the waitlist" is only true for the first.
- [ ] The offer email's call to action works for the attendee's actual situation after ticket 18 (a waitlisted or registered attendee claims the offer from their existing registration).

### Partner API contract

- [ ] The update-registration request field for the coupon code gets a source-neutral name, since it now accepts organiser coupons as well as waitlist coupons.
- [ ] The breaking change to partner update-registration (`ticketTypeIds` renamed to `registerTicketTypeIds`, plus the required `waitlistTicketTypeIds`) is documented for partner integrators, with a release note to deploy it together with the event-site change. Alternatively the old shape is accepted for a transition period; decide which and record it.

### Admin UI: waitlist page

- [ ] "Promote to VIP" asks for confirmation and explains that the offer can't be revoked, and that it's an admin ticket on top of public capacity: it doesn't use a public seat, and nobody else on the waitlist loses their place because of it.
- [ ] A failed entry removal shows an error toast instead of failing silently.
- [ ] A failed waitlist query shows an error state, not "No one is currently on the waitlist".
- [ ] Vitest coverage for the three states above.

### Coupons

- [ ] Creating a coupon with the same ticket type listed twice is rejected by validation (or the list is de-duplicated). Today redeeming such a coupon through update fails.
- [ ] Coupon list, detail and public detail queries use `TimeProvider` rather than the system clock, so a coupon's status is testable.
- [ ] Redeeming an organiser coupon for a ticket type where the same attendee holds an unredeemed waitlist offer settles that offer. Today it stays open, its seat stays held, and the attendee later gets an "offer expired" email for a ticket they already have. Settle it the same way as an admin registration in ticket 18: the ticket is an admin ticket (ticket 20), the offer expires without an email, and an automatic offer's public hold is released so the next person in the queue gets an offer.
- [ ] Decide whether coupon listing needs pagination; implement it or note why not.
- [ ] Decide whether organisers need coupon pages in the Admin UI (today coupons can only be created through the API). If so, split out a separate ticket rather than growing this one.

### Waitlist jobs and claim window

- [ ] The expired-coupon job saves each event's waitlists in its own unit of work, so one concurrency conflict doesn't roll back and retry the whole batch. Waitlists on inactive events are no longer re-queried on every run.
- [ ] The claim-window calculator either guarantees a waking-hours window (an evening offer doesn't expire during quiet hours) or its documentation is corrected. A quiet-hours end that falls in a DST gap doesn't throw. Offers never expire after the event starts.

### Tests

- [ ] Concurrent double redemption of one coupon: exactly one succeeds.
- [ ] API authorization tests for creating, listing and getting coupons (organiser only; other teams rejected).
- [ ] Persistence test: a missing `waitlist_queued_count` key reads as 0, matching the existing held-capacity and origin tests.

### Docs

- [ ] The arc42 runtime view no longer says waitlist-only submissions create no Registration or return a null registration id.
- [ ] CONTEXT.md gets glossary entries for Waitlisted (registration status), waitlist offer, VIP promotion and waitlist-held capacity. They follow ticket 20: VIP promotion gives an admin ticket on top of public capacity, and waitlist-held capacity counts outstanding automatic offers only. Public capacity and admin ticket are added by ticket 20.
