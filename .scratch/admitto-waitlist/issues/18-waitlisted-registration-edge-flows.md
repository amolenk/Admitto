# 18: Waitlisted registrations work in every registration flow

**What to build:** Ticket 01 gave every waitlist joiner a `Waitlisted` registration. Several flows still assume a live registration is always `Registered`. After this ticket, a waitlisted attendee can claim their waitlist offer, including after registration has closed if the offer was issued before. The waitlist stops issuing offers when registration closes. They can rejoin or re-register after an offer lapses. Organisers, partner sites and the check-in scanner also treat them correctly.

**Blocked by:** 20 (Public capacity is the only enforced limit). The capacity assertions below use its pools: automatic offers hold a public seat, and VIP offers and admin actions are admin tickets on top of public capacity.

**Status:** done (pre-deploy data check outstanding)

Found in the pre-release sanity check (2026-09-29). Every item below reproduces on current main, and none is caught by a test. The register-with-coupon fixtures add waitlist entries without the matching `Waitlisted` registration, so they miss the main case. Each fix needs a test that starts from a real `Waitlisted` registration.

### Claiming a waitlist offer

- [x] Registering with a coupon succeeds when the email already has a `Waitlisted` registration: the registration gains the coupon's tickets and becomes `Registered`, and its other waitlist entries stay intact. Today this gets a 409 ("Only a cancelled registration can be reset"). This applies to automatic and VIP offers alike.
- [x] Admin register for an email with a `Waitlisted` registration succeeds the same way. It removes the attendee's matching waitlist entry, as organiser-coupon redemption already does. The new tickets are admin tickets (ticket 20). If the attendee holds an outstanding automatic offer for a ticket type the admin registers them for, that offer expires and its public hold is released, so the seat goes to the next person in the queue. The admin registration does not redeem the offer, and no expired-offer email is sent: the attendee gets the registration's ticket email, and the coupon can't be used for a ticket type they already hold.
- [x] Redeeming a coupon through update-registration honours the coupon's `BypassRegistrationWindow`. An offer issued before registration closed can still be claimed after close, until the offer expires. Without a coupon, update-registration still enforces the registration window.
- [x] Registering with a coupon after registration has closed succeeds for a `Waitlisted` registration when the coupon bypasses the window.
- [x] Integration tests for each path above: register-with-coupon from `Waitlisted`, admin register from `Waitlisted`, and update-with-coupon after close. Each asserts the pool accounting from ticket 20, with no double count:
  - Redeeming an automatic offer: the offer's hold becomes a public ticket (`WaitlistHeldCapacity` −1, `PublicUsedCapacity` +1).
  - Redeeming a VIP offer: an admin ticket (`AdminUsedCount` +1); public counters unchanged.
  - Admin register while holding an automatic offer: an admin ticket; the offer expires, its hold is released, and the next person in the queue gets an offer.

### The waitlist stops at registration close

- [x] No automatic offers are issued at or after `RegistrationPolicy.ClosesAt`, whatever the trigger: a cancellation, an expired offer's released hold or a capacity raise. The check belongs in the `WaitlistCapacityAvailableDomainEvent` path, so every trigger is covered.
- [x] Offers issued before close stay valid until they expire (waitlist coupons keep bypassing the window). When one expires after close, its hold is released and nobody else gets an offer. The expired-offer email sent after close doesn't invite the attendee to register again.
- [x] The queue is left as it is at close: entries stay active, registrations stay `Waitlisted`, and no email is sent. VIP promotion still works after close (an admin action, on top of public capacity per ticket 20).
- [x] Moving `ClosesAt` later so that registration is open again re-runs the waitlist check for every ticket type in WaitlistMode, just as raising capacity does. Seats freed while registration was closed go to the queue straight away.
- [x] Integration tests:
  - A cancellation after close issues no offer.
  - An offer issued before close can be redeemed after close.
  - An offer expiring after close issues no new offer.
  - Raising capacity after close issues no offer.
  - Moving `ClosesAt` later issues offers for the seats freed while closed.
  - VIP promotion after close succeeds.
- [x] arc42 §6 and §8.14 describe the waitlist stopping at close and resuming when the window is reopened. arc42 §11 gets a tech-debt row for the accepted race: the offer decision reads `ClosesAt` from `TicketedEvent` but writes the `TicketCatalog`, so reopening the window at the same moment as a cancellation can leave one freed seat unoffered until the next waitlist trigger. This is accepted because it is rare, the seat stays publicly available and the next trigger fixes it.

### Self-service waitlist-only submissions

- [x] A waitlist-only submission from an email whose registration is `Cancelled` reactivates it as `Waitlisted` and sends the waitlist confirmation email. Today the entries are added to a registration that stays `Cancelled`.
- [x] A `Waitlisted` registration with no active waitlist entries left (offer expired, or entry removed by an organiser) no longer blocks the attendee from registering or rejoining. The expired-offer email's "register again" instruction works.
- [x] A waitlist-only submission from an email that is already `Registered` or `Waitlisted` with active entries behaves like the equivalent update: entries are added and a ticket-changed email is sent. Today the entries are added silently.
- [x] Tests for each of the three cases.

### Organiser, partner and scanner views

- [x] Admin registration detail page: the hero badge and Status row show "Waitlisted" for a waitlisted registration (today they show "Registered" and "Cancelled"). Cancel and Change tickets are available for waitlisted registrations. The page matches the registrations list's status handling. Vitest coverage.
- [x] Partner resolve-by-email finds `Waitlisted` registrations, so a waitlist-only attendee can update or cancel through the partner site. API test.
- [x] Check-in scanner: a waitlisted registration is not shown as Eligible. Check-in returns a dedicated "waitlisted" outcome instead of an error, and the scanner shows a clear message instead of "Network error… retry". Tests in the backend and the scanner UI.

### Before deploying

- [ ] Confirm that no environment has rows in the waitlists or coupons tables that predate the migrations from tickets 14, 16, 17 and 20. Those migrations don't backfill held capacity or queued count, don't carry revoked coupons over, and don't set `expires_at` on existing waitlist coupons, and ticket 20's migration doesn't convert reserved capacity or ticket modes. If any environment has such data, write a data migration first (derive the counts from active entries and outstanding automatic offers, map Revoked to Expired, copy `expires_at` from the coupons table, map ticket mode `Reserved` → `Admin` and `PublicUncapped` → `Public`). Record the outcome in this ticket.

  **Outcome (2026-10-01):** not yet checked. The implementation sandbox has no access to any deployed environment's database, so this needs someone with that access before deploying. Until then, treat it as open.

### Implementation notes

- Admin register withdraws any outstanding offer the attendee holds for a ticket type it registers them for, VIP offers included. A VIP offer holds no seat, so withdrawing it frees nothing; it just stops a useless offer keeping the waitlist out of "exhausted".
- An explicit waitlist disable after close offers no freed slots. Removing a ticket type's capacity limit after close still offers everyone waiting, because that is a deliberate "room for everyone" action by the organiser.
- An event with no registration window never closes the waitlist; clearing the window counts as moving `ClosesAt` later and re-runs the waitlist check.
- Update-registration with a window-bypassing coupon after close allows only what the coupon grants. Any other ticket or waitlist change in the same request (adding or releasing tickets, joining or leaving a waitlist) is still rejected as `registration.closed`.
- A `Waitlisted` registration holding an outstanding offer (its entry left the queue when the offer was issued) still counts as live in self-service, so the attendee can't register publicly alongside the seat their offer holds.
- `Coupon.GetStatus` treats a coupon as expired from `ExpiresAt` itself (`<=`, was `<`), matching `Coupon.Create` and the waitlist lapse check, so `Coupon.Expire(now)` takes effect immediately.

