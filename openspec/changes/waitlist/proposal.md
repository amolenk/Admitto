## Why

When a ticketed event sells out, attendees currently have no way to express interest
in obtaining a ticket if capacity opens up due to cancellations. A ranked waitlist
lets attendees queue up per ticket type and receive a time-limited claim offer when
freed capacity becomes available — maximising fill-rate and providing a fair,
transparent mechanism for sold-out events.

## What Changes

- A `WaitlistEnabled` flag is added per ticket type (organiser-configured, opt-in).
- When a `WaitlistEnabled` ticket type reaches full capacity the system enters
  **WaitlistOnly mode** for that ticket type: regular self-service registrations are
  blocked so no one can sneak past the queue between a cancellation and a coupon
  redemption. WaitlistOnly mode lifts only when capacity is available AND no active
  waitlist entries AND no unredeemed waitlist coupons remain.
- Attendees can join and leave the ranked waitlist for any ticket type in WaitlistOnly
  mode. Joining requires email verification via a signed link.
- When one or more tickets become available (via registration cancellation) the system
  distributes **system-generated single-use coupons** — one per freed slot — to the
  first N people on the waitlist (reusing existing coupon infrastructure).
- Coupons expire after a configurable claim window (default 8 hours). To prevent
  attendees waking up to a missed window, notifications are **not sent during quiet
  hours** (configurable, default 22:00–08:00 in the event's local timezone); if the
  processor fires during quiet hours it delays notification to the start of the next
  allowed window and anchors the claim window from there.
- Once notified, an attendee is **automatically removed** from the waitlist and must
  re-join if they wish to be considered again.
- If a coupon expires without being redeemed, the system notifies the next batch on
  the waitlist and repeats until all freed slots are claimed or the waitlist is
  exhausted.
- A new public **coupon details lookup** endpoint allows the external event website to
  resolve a coupon code and pre-select the allowed ticket type in the registration
  flow before the attendee even starts filling out the form.
- Organisers get an admin UI page to view and manage the waitlist per ticket type
  (active entries, pending notifications with remaining claim time, and quick removal).

## Capabilities

### New Capabilities

- `waitlist`: Attendee waitlist for sold-out ticket types — join/leave via signed
  email link, ranked coupon-based claim distribution, configurable claim window with
  quiet-hours support, and WaitlistOnly mode enforcement.
- `admin-ui-waitlist`: Admin UI page for organisers to view active waitlist entries
  per ticket type, monitor pending notifications, and remove entries.

### Modified Capabilities

- `ticket-type-management`: Add `waitlistEnabled` flag (default `false`) to ticket
  type create/update. Expose a derived `waitlistMode` status on ticket type listings
  so the public event site knows when to surface the "join waitlist" button.
- `coupon-management`: System-generated waitlist coupons (not organiser-created, no
  invitation email) tagged with `source: "waitlist"`. New public endpoint to look up
  coupon details by code so external sites can pre-select ticket types.
- `attendee-registration`: When a ticket type is in WaitlistOnly mode, self-service
  registration is rejected with reason `"ticket type in waitlist mode"` so the client
  can redirect attendees to the waitlist join flow.
- `event-management`: *(no change needed)* — `TicketedEvent` already carries a
  `TimeZone` field (IANA identifier) managed via the existing
  `UpdateTicketedEventTimeZone` endpoint; the waitlist processor reads it directly.

## Impact

- **Registrations module**: New `Waitlist` aggregate per ticket type tracking ordered
  entries and pending notifications. `WaitlistOnly` flag added to `TicketType` in
  `TicketCatalog`. New command handlers: `JoinWaitlist`, `LeaveWaitlist`,
  `ProcessWaitlistNotifications`. New domain events: `WaitlistEntryAdded`,
  `WaitlistEntryRemoved`, `WaitlistCouponsDistributed`, `WaitlistNotificationDelayed`.
  Cancellation flow triggers a `WaitlistNotificationRequested` module event.
  `TicketCatalog.TicketType.WaitlistEnabled` and `WaitlistMode` flags added.
- **Coupon aggregate**: New `CouponSource` discriminator (`Organiser` vs `Waitlist`).
  Waitlist coupons suppress the invitation-email trigger. New public query endpoint.
- **Email module**: New email template for waitlist notification (explains claim window
  and quiet-hours impact). Existing `IEventEmailFacade` extended with
  `SendWaitlistNotificationAsync`.
- **Worker host**: New Quartz job `ProcessExpiredWaitlistCouponsJob` polling for
  unredeemed expired waitlist coupons and triggering next-batch notification.
- **API**: New public endpoints for join/leave. New public coupon lookup endpoint.
  Existing attendee-registration endpoint updated to check WaitlistOnly mode.
  Admin endpoints for waitlist view and entry removal.
- **Admin UI**: New waitlist management page under each event's ticket type detail.
  Ticket type form gains `waitlistEnabled` toggle and `claimWindowHours` input.
  Event settings form gains `quietHoursStart` / `quietHoursEnd` fields (timezone
  already exists).
