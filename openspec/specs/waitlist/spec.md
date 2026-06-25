# Waitlist Specification

## Purpose

Attendees can join a ranked waitlist for a sold-out ticket type. When capacity is
released the system distributes single-use coupons to the first N people on the
waitlist. Notifications respect a configurable quiet-hours window so attendees are
not woken up only to miss their claim window. Once notified, an attendee is removed
from the waitlist and must re-join to be considered again.

## Requirements

### Requirement: Public waitlist endpoints use API-key team scope
The system SHALL expose public waitlist endpoints under `/api/events/{eventId}` and SHALL require a valid active `X-Api-Key` for each request. Public waitlist endpoint handlers SHALL derive `TeamId` from the authenticated API-key principal and SHALL NOT accept team ID or team slug in the URL.

Joining a waitlist SHALL be exposed at `POST /api/events/{eventId}/waitlist/{ticketTypeId}`. Leaving a waitlist SHALL be exposed at `DELETE /api/events/{eventId}/waitlist/{ticketTypeId}`.

#### Scenario: Join waitlist uses API-key team scope
- **WHEN** an attendee posts a valid waitlist join request to `POST /api/events/{eventId}/waitlist/{ticketTypeId}` with a valid API key for the event's team
- **THEN** the system processes the request using the API key owner's `TeamId`, the route `{eventId}`, and the route `{ticketTypeId}`

#### Scenario: Leave waitlist uses API-key team scope
- **WHEN** an attendee sends a valid leave request to `DELETE /api/events/{eventId}/waitlist/{ticketTypeId}` with a valid API key for the event's team
- **THEN** the system processes the request using the API key owner's `TeamId`, the route `{eventId}`, and the route `{ticketTypeId}`

#### Scenario: Waitlist endpoint without API key is rejected
- **WHEN** an attendee calls either public waitlist endpoint without `X-Api-Key`
- **THEN** the system returns HTTP 401 and does not run the waitlist handler

---

### Requirement: Attendee can join the waitlist for a ticket type in WaitlistOnly mode

The system SHALL allow an attendee to join the waitlist for a specific ticket type
that is in WaitlistOnly mode. The request MUST include a valid OTP verification token
that proves email ownership. The entry is added to the waitlist immediately — there is
no separate confirmation step. The attendee is assigned the next position in the ranked
queue. An attendee with the same email address SHALL NOT be added twice for the same
ticket type; a duplicate request SHALL succeed silently (idempotent).

The OTP verification token SHALL be the same short-lived HMAC-signed token used by
self-service registration. It SHALL use the configured verification-token signing
key and SHALL NOT depend on a per-event signing key.

#### Scenario: Successfully join the waitlist
- **WHEN** attendee "alice@example.com" submits a waitlist join request for ticket
  type "General Admission" on event "DevConf" which is in WaitlistOnly mode, including
  a valid OTP verification token proving ownership of "alice@example.com"
- **THEN** an active waitlist entry is created immediately for "alice@example.com" at
  the next queue position and the response is HTTP 202 Accepted

#### Scenario: Duplicate join request is silently accepted
- **WHEN** "alice@example.com" is already on the waitlist for "General Admission" and
  submits another join request with a valid token
- **THEN** the response is HTTP 202 Accepted with no duplicate entry created

#### Scenario: Invalid or expired token is rejected
- **WHEN** "alice@example.com" submits a waitlist join request with an invalid or
  expired verification token
- **THEN** the request is rejected with reason "verification token invalid or expired"

#### Scenario: Token email mismatch is rejected
- **WHEN** a join request contains a token issued for "bob@example.com" but the
  request specifies a different email address
- **THEN** the request is rejected (the token's email is authoritative; no email field
  is needed in the request body — the token carries it)

#### Scenario: Cannot join when ticket type is not in WaitlistOnly mode
- **WHEN** "alice@example.com" submits a waitlist join request for ticket type
  "General Admission" that is NOT in WaitlistOnly mode
- **THEN** the request is rejected with reason "ticket type not in waitlist mode"

#### Scenario: Cannot join when waitlistEnabled is false
- **WHEN** "alice@example.com" submits a waitlist join request for a ticket type
  with `WaitlistEnabled = false`
- **THEN** the request is rejected with reason "waitlist not enabled for this ticket type"

---

### Requirement: Attendee can leave the waitlist

The system SHALL allow an attendee to remove themselves from the waitlist at any time
via a signed leave link (included in all waitlist-related emails) or via a direct
remove endpoint. Leaving a waitlist the attendee is not on SHALL succeed silently
(idempotent).

#### Scenario: Successfully leave the waitlist
- **WHEN** "alice@example.com" requests removal from the waitlist for "General
  Admission" on "DevConf" while holding an active entry
- **THEN** the waitlist entry is removed and "alice@example.com" no longer appears in
  the queue

#### Scenario: Leave when not on waitlist is idempotent
- **WHEN** "alice@example.com" requests removal from the waitlist for "General
  Admission" on "DevConf" while not holding an active entry
- **THEN** the response is 200 OK and no error is raised

---

### Requirement: System notifies waitlisted attendees when capacity is released

When one or more registrations for a `WaitlistEnabled` ticket type are cancelled and the ticket type has WaitlistMode active, the system SHALL distribute one waitlist coupon per freed slot to the next N entries in the ranked waitlist. Each notification email SHALL be sent immediately. If the current time falls within the configured quiet hours for the event, the coupon's `ExpiresAt` is extended so that quiet hours do not count toward the claim window. Each notified attendee is removed from the active waitlist at the point of notification.

#### Scenario: Notify first batch when capacity is freed
- **WHEN** a registration for "General Admission" on "DevConf" is cancelled, freeing
  1 slot, and the waitlist has "alice@example.com" at position 1 and "bob@example.com"
  at position 2, and the current time is within the allowed notification window
- **THEN** one waitlist coupon is created for "alice@example.com" (valid for
  `ClaimWindowHours`), a notification email is sent to "alice@example.com" with the
  coupon code and expiry time, and "alice@example.com" is removed from the waitlist.
  "bob@example.com" remains at position 1.

#### Scenario: Multiple slots freed notifies multiple attendees
- **WHEN** 3 registrations for "General Admission" are cancelled simultaneously and
  the waitlist has 5 entries
- **THEN** coupons are issued to the first 3 waitlisted attendees, all three are
  notified by email, and all three are removed from the waitlist

#### Scenario: Fewer waitlist entries than freed slots
- **WHEN** 3 slots are freed but the waitlist has only 1 active entry
- **THEN** 1 coupon is issued to the single waiting attendee; the 2 remaining freed
  slots become regularly available once WaitlistMode conditions are re-evaluated

#### Scenario: Notification during quiet hours — email sent immediately with extended expiry
- **WHEN** a cancellation is processed at 23:00 in the event's local timezone and
  quiet hours are 22:00–08:00 and `ClaimWindowHours = 8`
- **THEN** the notification email is sent immediately at 23:00; the coupon's
  `ExpiresAt` is set to 16:00 the next day (08:00 + 8 hours) so the attendee has the
  full 8-hour claim window during waking hours

#### Scenario: Waitlist is empty — no notification sent
- **WHEN** a registration is cancelled and the waitlist for that ticket type is empty
- **THEN** no coupon is created and no email is sent; capacity becomes available and
  WaitlistMode is re-evaluated

---

### Requirement: System re-notifies when a claim window expires

When a waitlist coupon's claim window expires without redemption the system SHALL revoke the coupon and trigger notification for the next batch of waitlist entries (subject to the same quiet-hours rules).

Waitlist coupon redemption SHALL apply the offered ticket as a capacity grant. If the attendee has no active registration for the event, the grant can create a registration. If the attendee already has an active registration for the event, the grant can be applied by changing that registration's tickets. In both cases, the final persisted registration ticket set SHALL be valid.

#### Scenario: Coupon expires — next batch notified
- **WHEN** the waitlist coupon issued to "alice@example.com" reaches its `ExpiresAt`
  without being redeemed, and "bob@example.com" is the next entry on the waitlist
- **THEN** the coupon is revoked, a new coupon is issued to "bob@example.com", a
  notification email is sent to "bob@example.com", and "bob@example.com" is removed
  from the waitlist

#### Scenario: Coupon expires — waitlist exhausted, capacity restored
- **WHEN** the last waitlist coupon expires without redemption and the waitlist is empty
- **THEN** the coupon is revoked, WaitlistMode conditions are re-evaluated, and if
  capacity is available and no pending coupons remain, WaitlistMode is lifted

#### Scenario: Coupon redeemed — first registration created
- **WHEN** "alice@example.com" has no active registration and redeems a waitlist coupon before `ExpiresAt`
- **THEN** a registration is created with the offered ticket, the coupon is marked redeemed, and no further waitlist processing occurs for that slot

#### Scenario: Coupon redeemed — existing registration changed
- **WHEN** "alice@example.com" already has an active registration and redeems a waitlist coupon before `ExpiresAt` by submitting a valid final ticket selection that includes the offered ticket
- **THEN** the existing registration's tickets are changed, the coupon is marked redeemed, and no further waitlist processing occurs for that slot

---

### Requirement: Waitlist entries are independent ticket-type intents
Waitlist entries SHALL be independent per ticket type. The system SHALL allow an attendee to hold active waitlist entries for multiple ticket types even when those ticket types overlap each other or overlap the attendee's current registered tickets.

The system SHALL enforce time-slot overlap constraints only when creating or changing an actual Registration. A waitlist offer claim SHALL validate the final registered ticket set at claim time.

#### Scenario: Waitlist entry overlaps current registration
- **WHEN** an attendee is registered for "Workshop A" and joins the waitlist for overlapping "Workshop B"
- **THEN** the waitlist entry for "Workshop B" is accepted if the waitlist is active

#### Scenario: Multiple waitlist entries overlap each other
- **WHEN** an attendee joins waitlists for "Workshop B" and "Workshop C" and those ticket types share the same time slot
- **THEN** both waitlist entries are accepted if each waitlist is active

#### Scenario: Waitlist offer claim validates final registration
- **WHEN** an attendee with current ticket "Workshop A" claims a waitlist offer for overlapping "Workshop B"
- **THEN** the claim succeeds only if the final submitted registration ticket set removes the overlap or otherwise satisfies registration ticket validation

---

### Requirement: Registration submission can join selected waitlists atomically
The public registration submission SHALL be able to create waitlist entries for all ticket types listed in `waitlistTicketTypeIds` in the same transaction as any registration created for `registerTicketTypeIds`.

Each requested waitlist ticket type SHALL currently have `WaitlistEnabled = true` and `WaitlistMode = true`. If any requested waitlist action cannot be applied, the system SHALL reject the entire submission and SHALL NOT create a partial registration or partial waitlist entries.

#### Scenario: Mixed submission creates waitlist entry atomically
- **WHEN** an attendee submits registration for "Workshop A" and waitlist join for "Workshop B" in one public registration request
- **THEN** the registration and waitlist entry are persisted in the same transaction

#### Scenario: Mixed submission rejects stale waitlist state
- **WHEN** an attendee submits waitlist join for "Workshop B" but Workshop B has left WaitlistMode before submission is handled
- **THEN** the entire submission is rejected and no registration or waitlist entry is created

---

### Requirement: WaitlistOnly mode is enforced on ticket types at capacity

When a ticket type has `WaitlistEnabled = true` and reaches capacity the system SHALL
activate WaitlistOnly mode automatically. The mode persists until all three of the
following conditions hold: (1) available capacity > 0, (2) no active waitlist entries,
(3) no unredeemed non-expired waitlist coupons for this ticket type.

#### Scenario: WaitlistOnly mode activates when capacity is reached
- **WHEN** the last available slot for "General Admission" on "DevConf" is registered
  and "General Admission" has `WaitlistEnabled = true`
- **THEN** `TicketType.WaitlistMode` is set to `true` and subsequent self-service
  registrations for "General Admission" are rejected with reason
  "ticket type in waitlist mode"

#### Scenario: WaitlistOnly mode does not activate when WaitlistEnabled is false
- **WHEN** the last available slot for "VIP Pass" on "DevConf" is registered and
  "VIP Pass" has `WaitlistEnabled = false`
- **THEN** `TicketType.WaitlistMode` remains `false`; subsequent self-service
  registrations are rejected with the standard "ticket type at capacity" reason

#### Scenario: WaitlistOnly mode lifts when waitlist is exhausted and capacity is available
- **WHEN** all waitlist entries for "General Admission" are notified, all coupons are
  redeemed or expired, and available capacity > 0
- **THEN** `TicketType.WaitlistMode` is set to `false` and self-service registrations
  for "General Admission" are accepted again

---

### Requirement: Waitlist claim window and quiet hours are configurable per ticket type

Organisers SHALL be able to configure a `ClaimWindowHours` (integer, minimum 1,
default 8) per ticket type. The event-level `QuietHoursStart` and `QuietHoursEnd`
(time-of-day values, defaulting to `22:00` and `08:00`) apply to all ticket type
waitlists for that event. Quiet-hours evaluation uses the event's `TimeZoneId` (IANA
identifier); if not set, UTC is assumed.

#### Scenario: Custom claim window is respected
- **WHEN** "General Admission" has `ClaimWindowHours = 12` and a coupon is issued at
  10:00
- **THEN** the coupon's `ExpiresAt` is 22:00 the same day (10:00 + 12 hours)

#### Scenario: Quiet hours shift the claim window end, not the send time
- **WHEN** a coupon would be issued at 23:00 with `ClaimWindowHours = 8` and
  quiet hours are 22:00–08:00
- **THEN** the coupon IS issued at 23:00 and `ExpiresAt` is 16:00 the next day
  (max(23:00, 08:00) + 8h = 08:00 + 8h = 16:00); the email states the deadline as 16:00
