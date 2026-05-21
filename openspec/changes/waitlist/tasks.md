# Implementation Tasks — Waitlist

## Group 1: WaitlistEnabled flag and WaitlistOnly mode on TicketCatalog

### Task 2.1: Add WaitlistEnabled and WaitlistMode to TicketType value object
- Add `WaitlistEnabled` (bool, default false) and `WaitlistMode` (bool, default false) to the `TicketType` value object inside `TicketCatalog`.
- Add `ClaimWindowHours` (int, minimum 1, default 8) to `TicketType`.
- Update EF Core entity configuration for new columns.

### Task 2.2: Add WaitlistEnabled to ticket type add/update commands
- Add `WaitlistEnabled` and `ClaimWindowHours` to `AddTicketType` and `UpdateTicketType` commands, handlers, HTTP requests, and FluentValidation validators.
- Guard: `WaitlistEnabled` can only be true when `Capacity` is set (unlimited-capacity types cannot sell out).
- **Retroactive activation**: if `UpdateTicketType` sets `WaitlistEnabled = true` and `UsedCapacity >= MaxCapacity`, raise `WaitlistModeActivatedDomainEvent` immediately (same path as `ClaimCapacity`).
- **Force-disable cleanup**: if `UpdateTicketType` sets `WaitlistEnabled = false` while `WaitlistMode = true`, raise a `WaitlistForcedDisabledDomainEvent`. The handler: (a) revokes all pending waitlist coupons and queues a "claim cancelled" email per holder, (b) marks all active waitlist entries as Removed, (c) calls `TicketCatalog.ForceDeactivateWaitlistMode(ticketTypeId)` which always clears `WaitlistMode` regardless of capacity.
- **Capacity increase while in WaitlistMode**: if `UpdateTicketType` increases `MaxCapacity` and the ticket type has `WaitlistMode = true`, compute freed slots (`newMax - usedCapacity`) and call `ProcessWaitlistNotifications` with that count.
- **Remove capacity limit while WaitlistEnabled**: if `UpdateTicketType` removes the capacity constraint (unlimited) on a ticket type with `WaitlistEnabled = true`, treat it as a force-disable (same cleanup path as above).

### Task 2.3: Activate WaitlistOnly mode when capacity is reached
- In `TicketCatalog.ClaimCapacity` (called on registration), after incrementing used capacity, set `WaitlistMode = true` if `WaitlistEnabled && UsedCapacity >= MaxCapacity`.

### Task 2.4: Re-evaluate and lift WaitlistOnly mode
- Add a domain method `TicketCatalog.ReEvaluateWaitlistMode(ticketTypeId, activeWaitlistCount, activeWaitlistCouponCount)` that sets `WaitlistMode = false` when used < max AND active entries == 0 AND active coupons == 0.
- Call this after: (a) successful coupon redemption/registration, (b) coupon expiry cycle, (c) admin removal of waitlist entry.

### Task 2.5: Expose WaitlistMode in ticket type listing and detail responses
- Add `waitlistMode` and `waitlistEnabled` booleans to the ticket type listing/detail API response.

### Task 2.6: Tests for WaitlistEnabled and WaitlistOnly mode
- Domain test: WaitlistMode activates when last slot is taken and WaitlistEnabled is true.
- Domain test: WaitlistMode does not activate when WaitlistEnabled is false.
- Domain test: enabling WaitlistEnabled on a sold-out ticket type immediately activates WaitlistMode (retroactive activation).
- Domain test: disabling WaitlistEnabled while WaitlistMode is active clears WaitlistMode and removes all entries regardless of capacity.
- Domain test: increasing MaxCapacity while WaitlistMode active triggers ProcessWaitlistNotifications for freed slots.
- Domain test: removing capacity limit while WaitlistEnabled force-disables WaitlistEnabled.
- Domain test: WaitlistMode lifts when conditions are met (capacity, no entries, no pending coupons).
- Integration test: add ticket type with `waitlistEnabled: true`; fill to capacity; verify `waitlistMode: true` in response.
- Integration test: enable waitlist on sold-out ticket type; verify WaitlistMode activates immediately.
- Integration test: force-disable WaitlistEnabled while entries and pending coupons exist; verify cleanup and cancellation emails.

---

## Group 3: Waitlist aggregate and core domain

### Task 3.1: Create the Waitlist aggregate
- Define `Waitlist` aggregate root: `WaitlistId` (EventId + TicketTypeId), `Entries`
  (ordered by position), `PendingCouponCount` (int, tracks issued-but-not-resolved coupons).
- Each entry: `WaitlistEntryId` (GUID), `Email`, `Position` (int), `AddedAt` (UTC), `Status` (Active / Removed).
- Add EF Core entity configuration and migration.
- Add `WaitlistRepository` in the Registrations module.

### Task 3.2: Activate Waitlist aggregate via domain event from TicketCatalog
- In `TicketCatalog.ClaimCapacity`, when `WaitlistEnabled && UsedCapacity >= MaxCapacity`, set `WaitlistMode = true` and raise `WaitlistModeActivatedDomainEvent`.
- Add `WaitlistModeActivatedDomainEventHandler`: creates a new `Waitlist` aggregate for the ticket type in the same `RegistrationsDbContext` (runs in `SavingChangesAsync`, same transaction).

### Task 3.3: Implement JoinWaitlist command
- Command: `JoinWaitlist { EventId, TicketTypeId, Email }`.
- Handler: validate ticket type exists, WaitlistEnabled, WaitlistMode active; check for existing active entry (idempotent — return success without sending new email); otherwise add entry and raise `WaitlistEntryAdded` domain event.
- Domain event triggers `SendWaitlistVerificationEmail` via the Email module facade.
- Verification link: HMAC-signed URL (similar to existing HMAC token pattern) valid 24 hours, pointing to the verification endpoint.

### Task 3.4: Implement email-verified waitlist confirmation endpoint
- New public endpoint `POST /events/{teamSlug}/{eventSlug}/waitlist/{ticketTypeId}/confirm?token={hmac}`.
- Validates HMAC token; creates active waitlist entry if not already active; assigns next position.

### Task 3.5: Implement LeaveWaitlist command
- Command: `LeaveWaitlist { EventId, TicketTypeId, Email }`.
- Removes the active entry (idempotent — no error if not found).
- Raises `WaitlistEntryRemoved` domain event.
- After removal, calls `ReEvaluateWaitlistMode`.

### Task 3.6: Deactivate WaitlistMode via domain event from Waitlist aggregate
- Add `WaitlistExhaustedDomainEvent` raised by `Waitlist` when both `Entries.Count(Active) == 0` and `PendingCouponCount == 0`.
- Add `WaitlistExhaustedDomainEventHandler`: loads `TicketCatalog` into the same `RegistrationsDbContext` and calls `TicketCatalog.TryDeactivateWaitlistMode(ticketTypeId)`, which clears `WaitlistMode` only when `UsedCapacity < MaxCapacity` also holds.
- Add `TicketCatalog.ForceDeactivateWaitlistMode(ticketTypeId)`: unconditionally clears `WaitlistMode`; used by the admin force-disable path regardless of capacity.
- Add `CouponRedeemedDomainEventHandler` (in Registrations module): when a `Waitlist`-sourced coupon is redeemed, decrements `Waitlist.PendingCouponCount`; this may trigger `WaitlistExhaustedDomainEvent`.

### Task 3.7: Admin remove waitlist entry endpoint
- New admin endpoint `DELETE /admin/teams/{teamId}/events/{eventId}/ticket-types/{ticketTypeId}/waitlist/{entryId}`.
- Calls `LeaveWaitlist` (or a dedicated `AdminRemoveWaitlistEntry` command).

### Task 3.8: Tests for waitlist domain logic
- Domain test: joining adds an entry at the correct position.
- Domain test: joining twice is idempotent.
- Domain test: cannot join when WaitlistMode is false.
- Domain test: leaving removes the entry and re-numbers positions.
- Domain test: leaving when not on waitlist is idempotent.
- Integration test: full join flow including email verification.
- Domain test: `WaitlistExhaustedDomainEvent` fires when both entries and `PendingCouponCount` are zero.
- Domain test: `TryDeactivateWaitlistMode` does not clear the flag when capacity is still full.
- Domain test: `TryDeactivateWaitlistMode` clears the flag when capacity is available and waitlist is exhausted.

---

## Group 4: WaitlistOnly mode enforcement in attendee registration

### Task 4.1: Block self-service registration when WaitlistMode is active
- In `TicketCatalog.ClaimCapacity` (or the registration pre-check), if `TicketType.WaitlistMode = true` and no coupon bypass, return domain error with reason `"ticket type in waitlist mode"`.
- Map this to a distinct rejection reason in the registration response.

### Task 4.2: Tests for WaitlistOnly mode enforcement
- Domain test: self-service registration returns "ticket type in waitlist mode" when active.
- Domain test: coupon-based registration succeeds even when WaitlistMode is active.
- Integration test: self-service registration is rejected with the correct reason.

---

## Group 5: Coupon source discriminator and system-generated waitlist coupons

### Task 5.1: Add CouponSource discriminator to Coupon aggregate
- Add `Source` enum (`Organiser`, `Waitlist`) to the `Coupon` aggregate.
- Existing organiser-created coupons use `Organiser`; new system-generated coupons use `Waitlist`.
- `Waitlist` source coupons suppress the invitation-email trigger in `CouponCreated` handler.
- Update EF Core entity configuration; add migration.

### Task 5.2: Expose CouponSource in coupon list response
- Add `source` field to coupon list and detail responses.

### Task 5.3: Tests for CouponSource
- Domain test: organiser-created coupon triggers invitation email.
- Domain test: waitlist coupon does NOT trigger invitation email.
- Integration test: coupon list includes correct `source` for each type.

---

## Group 6: Waitlist notification flow

### Task 6.1: Implement ProcessWaitlistNotifications command
- Command: `ProcessWaitlistNotifications { EventId, TicketTypeId, FreedSlots }`.
- Handler:
  1. Load event's `TimeZone`, `QuietHoursStart`, and `QuietHoursEnd`.
  2. Compute `effectiveStart = max(utcNow, nextAllowedWindowStart(utcNow, timeZone, quietHours))`.
  3. Take up to `FreedSlots` active waitlist entries in position order.
  4. For each: generate a `Waitlist` coupon with `ExpiresAt = effectiveStart + ClaimWindowHours`, remove the entry from the waitlist.
  5. Send a waitlist notification email per attendee including the coupon code and the `ExpiresAt` deadline.
  6. Call `ReEvaluateWaitlistMode` after processing.
  7. No delayed-scheduling is needed; the expiry job already handles the next-batch cascade.

### Task 6.2: Trigger ProcessWaitlistNotifications on cancellation
- In the `RegistrationCancelled` domain event handler, if the cancelled ticket type has `WaitlistEnabled`, call `ProcessWaitlistNotifications` for the freed slots.

### Task 6.3: Add event-level quiet hours configuration
- Add `QuietHoursStart` and `QuietHoursEnd` (TimeOnly) fields to `TicketedEvent` (defaulting to 22:00 and 08:00). The existing `TimeZone` field is already present.
- Expose in the event update endpoint (alongside the existing timezone endpoint pattern).
- Add a helper `WaitlistClaimWindowCalculator.ComputeExpiresAt(utcNow, timeZone, quietHours, claimWindowHours)` that returns `max(utcNow, nextAllowedWindowStart) + claimWindowHours`.

### Task 6.4: Email template for waitlist notification
- Add `SendWaitlistNotificationAsync(email, couponCode, ticketTypeName, expiresAt)` to `IEventEmailFacade`.
- Create the email template: explains the claim offer, includes the coupon code, states the expiry time, and includes a direct registration link with the coupon pre-filled.
- If the quiet-hours delay was applied, the email should note "Your claim window opens at {NotifyAfter local time}."

### Task 6.5: Tests for notification flow
- Domain test: notification distributes correct number of coupons with correct `ExpiresAt` (outside quiet hours: `now + claimWindowHours`).
- Domain test: notification during quiet hours sets `ExpiresAt = nextAllowedWindowStart + claimWindowHours` (email still sent immediately).
- Integration test: cancel a registration → waitlist is notified → coupon created with correct expiry → email sent.
- Integration test: notification during quiet hours produces extended expiry, not a delayed send.

---

## Group 7: Expiry processing and next-batch cascade

### Task 7.1: Implement ProcessExpiredWaitlistCouponsJob (Quartz job)
- New `ProcessExpiredWaitlistCouponsJob` in the Worker host polling every 5 minutes.
- For each expired, unredeemed waitlist coupon:
  1. Revoke the coupon.
  2. Call `ProcessWaitlistNotifications` for the owning ticket type (1 freed slot).
  3. Call `ReEvaluateWaitlistMode`.

### Task 7.2: Tests for expiry processing
- Integration test: coupon expires → next person on waitlist is notified.
- Integration test: last coupon expires, waitlist empty → WaitlistMode is lifted.

---

## Group 8: Public coupon lookup endpoint

### Task 8.1: Implement public GET /events/{teamSlug}/{eventSlug}/coupons/{couponCode}
- New unauthenticated endpoint returning `{ status, allowedTicketTypes: [{ id, name }], expiresAt }`.
- 404 when coupon does not exist for the given event slug.
- Target email is NOT included in the response.

### Task 8.2: Tests for coupon lookup
- Integration test: look up active waitlist coupon returns correct status and ticket types.
- Integration test: look up redeemed coupon returns `status: "redeemed"`.
- Integration test: non-existent coupon code returns 404.
- Integration test: coupon from different event returns 404.

---

## Group 9: Admin UI — Ticket type form changes

### Task 9.1: Add WaitlistEnabled toggle to ticket type form
- Add `waitlistEnabled` toggle to the ticket type create/edit form in the Admin UI.
- Show only when capacity is set.
- Add `claimWindowHours` number input (shown when `waitlistEnabled` is on).
- Regenerate API client SDK after backend endpoints are updated.

---

## Group 10: Admin UI — Event settings form changes

### Task 10.1: Add TimeZoneId selector and quiet hours to event settings
- Add a searchable IANA timezone dropdown for `timeZoneId`.
- Add `quietHoursStart` and `quietHoursEnd` time pickers.
- Regenerate API client SDK after backend endpoints are updated.

---

## Group 11: Admin UI — Waitlist management page

### Task 11.1: Implement waitlist management page per ticket type
- New page: `/admin/teams/{teamId}/events/{eventId}/ticket-types/{ticketTypeId}/waitlist`.
- Show active entries (ranked, masked email, join date).
- Show pending notifications (masked email, expiry countdown).
- Show summary stats (total waiting, total pending, sent today).
- "Remove" button per active entry (calls admin remove endpoint).
- Regenerate API client SDK; use generated API functions in proxy routes.
