## Design Decisions

### Decision 1: WaitlistEnabled is an opt-in flag per ticket type

Waitlist behaviour is not automatically activated for every ticket type. Organisers
must explicitly enable it per ticket type via a `WaitlistEnabled` boolean flag
(default `false`). This keeps the default flow simple and lets organisers choose
which ticket types (e.g., General Admission but not VIP) should have a waitlist.

`WaitlistEnabled` can only be set to `true` when the ticket type has a bounded
capacity (unlimited-capacity types can never sell out). If the organiser later removes
the capacity limit, `WaitlistEnabled` is automatically forced to `false` (with
WaitlistMode cleanup if active — see Decision 2).

**Retroactive activation**: if `WaitlistEnabled` is toggled to `true` on a ticket type
that is already at capacity, the system immediately activates WaitlistMode and creates
the `Waitlist` aggregate in the same transaction, exactly as if the last slot had just
been claimed. This ensures there is no window where a sold-out ticket type has
`WaitlistEnabled = true` but `WaitlistMode = false`.

**Accepted**

---

### Decision 2: WaitlistOnly mode is a system-managed state that blocks regular self-service

When a `WaitlistEnabled` ticket type reaches its capacity limit, the system
automatically flips `TicketType.WaitlistMode = true` on the `TicketCatalog`
aggregate. While active, the self-service registration endpoint rejects attempts to
register for that ticket type with reason `"ticket type in waitlist mode"`.

**Automatic deactivation** — WaitlistMode is cleared when **all three** conditions
hold simultaneously:

1. Available capacity > 0 (used < max), **and**
2. No active (non-removed) waitlist entries for this ticket type, **and**
3. No unredeemed, non-expired waitlist coupons for this ticket type.

Condition 3 prevents a gap between coupon issuance and redemption where a
non-waitlisted user could register before the notified attendee does.

**Capacity increase while in WaitlistMode** — if the organiser increases `MaxCapacity`
on a ticket type that is in WaitlistMode and the new capacity creates free slots
(`newMax - usedCapacity > 0`), the system calls `ProcessWaitlistNotifications` for
the freed slot count immediately. This propagates the newly available capacity to the
front of the waitlist without waiting for the next expiry cycle. If the waitlist is
already empty (no active entries and no pending coupons), WaitlistMode conditions are
re-evaluated and the mode may lift. If there are pending coupons already in flight,
WaitlistMode stays active until those coupons are redeemed or expire.

**Admin force-disable** — the organiser may set `WaitlistEnabled = false` on a ticket
type that is currently in WaitlistMode. Rather than blocking this action, the system
performs a coordinated cleanup in the same transaction:

1. Revoke all pending (non-expired, non-redeemed) waitlist coupons for this ticket type;
   coupon holders receive a "your claim window has been cancelled" email.
2. Mark all active waitlist entries as Removed.
3. Clear `WaitlistMode = false`.
4. Set `WaitlistEnabled = false`.

This gives the organiser full control (e.g., event cancelled, large capacity addition
makes the waitlist redundant) without requiring the natural exhaustion path to complete.

**Accepted**

---

### Decision 3: System-generated single-use coupons as the claim mechanism

Rather than introducing a new claim endpoint, the system reuses the existing
`Coupon` aggregate to represent a claim offer. When waitlist notifications are
dispatched, the system creates one coupon per notified attendee with:

- `TargetEmail` = attendee's registered email
- `AllowedTicketTypes` = [the specific waitlisted ticket type]
- `ExpiresAt` = notification time + `ClaimWindowHours` (the end of the claim window), adjusted for quiet hours per Decision 5
- `Source` = `Waitlist` (new discriminator; suppresses invitation email)
- `BypassCapacity` = `true` (existing coupon behaviour)

The attendee redeems the offer through the standard coupon registration flow
(`POST /events/{teamSlug}/{eventSlug}/registrations` with the coupon code). No new
claim endpoint is needed.

**Accepted** — reuses battle-tested coupon infrastructure; external site can parse
the coupon code through the new public lookup endpoint to pre-select the ticket type.

---

### Decision 4: Claim window is configurable with an event-level default (8 hours)

The claim window duration is stored as an integer number of hours on the waitlist
configuration of each ticket type (`ClaimWindowHours`, default `8`). The window
begins at the moment the notification email is sent (or would be sent if not for
quiet hours — see Decision 5). After the window expires the coupon is revoked and
the next batch is notified.

**Accepted**

---

### Decision 5: Quiet hours extend the claim window expiry rather than delaying the notification

To avoid attendees waking up to find they missed their claim window overnight, the
system sends the notification email immediately (no delayed scheduling) but extends
`ExpiresAt` so that quiet hours do not count toward the claim window. The rule is:

```
effectiveStart = max(issuedAt, nextAllowedWindowStart)
ExpiresAt      = effectiveStart + ClaimWindowHours
```

where `nextAllowedWindowStart` is the start of the first non-quiet period at or after
`issuedAt`, evaluated in the **event's local timezone** (see Decision 8). If
`issuedAt` is already outside quiet hours, `effectiveStart == issuedAt` and no
extension occurs.

**Example**: coupon issued at 23:00, quiet hours 22:00–08:00, `ClaimWindowHours = 8`.
`nextAllowedWindowStart = 08:00` next morning → `ExpiresAt = 08:00 + 8h = 16:00`.
The email sent at 23:00 states the deadline as 16:00 the next day.

This approach is simpler to implement than delayed scheduling: no future jobs need to
be created per notification; the expiry job already handles the cascade. The attendee
receives the email promptly and always has the full claim window during waking hours.

Default quiet hours: 22:00–08:00. Configurable at the event level.

**Accepted**

---

### Decision 6: Capacity is soft-reserved via BypassCapacity, not hard-decremented

When a coupon is issued no capacity slot is immediately decremented from
`UsedCapacity`. Instead, `BypassCapacity = true` on the coupon allows the holder to
register even when the live capacity check would otherwise block them. This means
every coupon holder in a batch is effectively guaranteed a ticket — available
capacity does not need to be confirmed at redemption time because the bypass removes
that gate entirely.

The distinction matters for the `WaitlistMode` deactivation check (Decision 2,
condition 3): slots soft-reserved by outstanding coupons are not yet reflected in
`UsedCapacity`, so WaitlistMode must stay active until those coupons are redeemed
or expire — otherwise a non-waitlisted attendee could register ahead of a notified
attendee who has not yet redeemed.

The policy that a notified attendee is removed from the waitlist still applies
regardless of whether they redeem their coupon.

**Accepted**

---

### Decision 7: Notified attendee is removed from the waitlist regardless of redemption outcome

The moment a waitlist coupon is issued for an attendee they are removed from the
active waitlist for that ticket type. This matches the stated behaviour ("once
notified you must re-join"). It also prevents a state where an attendee holds both
a pending coupon and an active waitlist entry, which would cause them to be notified
twice.

**Accepted**

---

### Decision 8: Event timezone drives quiet-hours evaluation; already present on TicketedEvent

`TicketedEvent` already carries a `TimeZone` field (IANA identifier, e.g.
`"Europe/Amsterdam"`, managed via the existing `UpdateTicketedEventTimeZone` endpoint).
No new field is needed. Quiet-hours evaluation reads `TicketedEvent.TimeZone` directly.
If not set, UTC is assumed (attendees may see notifications at unsocial hours). No new
`event-management` capability delta is required by this change.

**Accepted**

---

### Decision 9: Coupon lookup endpoint is public and returns allowlisted ticket type details

External event websites need to parse a received coupon code and pre-select the
correct ticket type in their registration form. A new unauthenticated endpoint
`GET /events/{teamSlug}/{eventSlug}/coupons/{couponCode}` returns:

- Coupon status (active / expired / redeemed / revoked)
- Allowlisted ticket type IDs and names
- Expiry datetime

The target email is not returned. Because coupon codes are UUIDs the endpoint is
safe to expose publicly without leaking attendee information.

**Accepted**

---

### Decision 10: Waitlist is a separate aggregate per ticket type; domain events coordinate with TicketCatalog

Each ticket type with `WaitlistEnabled = true` gets its own `Waitlist` aggregate
(identified by `EventId` + `TicketTypeId`) that owns the ordered entry collection
and a collection of `WaitlistCoupon` records (one per coupon issued by the waitlist
mechanism). Each `WaitlistCoupon` tracks the coupon ID and its state: `Issued`,
`Redeemed`, or `Revoked`. The count of pending coupons is derived from the collection
(entries in `Issued` state) rather than stored as a raw counter. Separating the
aggregate gives it a clear identity, its own consistency boundary, and avoids
inflating `TicketCatalog` with waitlist state.

`TicketCatalog` retains two flags on `TicketType`: `WaitlistEnabled` (organiser
setting) and `WaitlistMode` (system-managed) — the latter is needed there because
self-service registration checks happen against `TicketCatalog`.

**Identifying waitlist coupons**: the `Coupon` aggregate carries `Source = Waitlist`
(see Decision 3). The registration command handler checks this discriminator before
interacting with the `Waitlist` aggregate, ensuring that admin-provisioned coupons
for the same ticket type are never treated as waitlist coupons.

**Activation** (cross-aggregate, in-transaction):
`TicketCatalog.ClaimCapacity` detects `WaitlistEnabled && UsedCapacity >= MaxCapacity`,
sets `WaitlistMode = true`, and raises a `WaitlistModeActivatedDomainEvent`. The
`DomainEventsInterceptor` dispatches this synchronously inside `SavingChangesAsync`
(before the actual DB write), so the handler creates the `Waitlist` aggregate into
the same scoped `RegistrationsDbContext`. Both changes are committed in one
transaction.

**Deactivation** (cross-aggregate, in-transaction):
The `Waitlist` aggregate raises `WaitlistExhaustedDomainEvent` whenever both its
active entry count and issued-coupon count reach zero. The event handler loads
`TicketCatalog` into the same scoped `DbContext` and calls
`TicketCatalog.TryDeactivateWaitlistMode(ticketTypeId)`, which clears `WaitlistMode`
only when `UsedCapacity < MaxCapacity` also holds. If capacity is still full (e.g.,
all pending coupons were admin-revoked but all slots remain occupied), `WaitlistMode`
stays active — correct, because new attendees can still join the now-empty waitlist.
Again, all mutations are committed in one `SaveChangesAsync` call.

**Coupon lifecycle calls on `Waitlist`** (both `TicketCatalog` and `Waitlist` share
the same `RegistrationsDbContext`, so no event indirection is needed):
- Registration command handler: calls `waitlist.RedeemCoupon(couponId)` after
  confirming `coupon.Source == Waitlist`.
- Expiry job: calls `waitlist.RevokeCoupon(couponId)` when revoking an expired
  waitlist coupon.

**Accepted**

---

### Decision 11: Expiry processing is handled by a dedicated Quartz job in the Worker host

A new `ProcessExpiredWaitlistCouponsJob` polls at a configurable interval (default
every 5 minutes) for waitlist coupons that have expired without being redeemed. When
found, it revokes the coupon and fires a `ProcessWaitlistNotifications` command to
attempt the next batch notification. This job also re-evaluates WaitlistMode after
each expiry cycle in case the waitlist has been exhausted.

**Race condition guard**: the job queries for coupons where
`ExpiresAt <= now - graceperiod` (default 2 minutes) rather than `ExpiresAt <= now`.
This eliminates the practical window where the expiry job and a last-second
redemption run concurrently against the same coupon. As a second line of defence,
the `Coupon` aggregate carries an EF Core concurrency token: if both transactions
commit simultaneously, the one that writes second will receive a
`DbUpdateConcurrencyException`. The registration handler treats this as "coupon no
longer valid" and returns an appropriate error to the attendee.

**Accepted**
