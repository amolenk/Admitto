# Implementation Tasks — Waitlist

## 1. WaitlistEnabled flag and WaitlistOnly mode on TicketCatalog

- [x] 1.1 Add `WaitlistEnabled` (bool, default false), `WaitlistMode` (bool, default false), and `ClaimWindowHours` (int, min 1, default 8) to `TicketType` value object; update EF Core entity configuration for new columns
- [x] 1.2 Add `WaitlistEnabled` and `ClaimWindowHours` to `AddTicketType`/`UpdateTicketType` commands, handlers, HTTP requests, and validators; guard: `WaitlistEnabled` requires bounded capacity; handle retroactive activation (sold-out type), force-disable cleanup (with `WaitlistForcedDisabledDomainEvent`), capacity-increase-while-in-WaitlistMode (trigger `ProcessWaitlistNotifications`), and remove-capacity-limit (treat as force-disable)
- [x] 1.3 In `TicketCatalog.ClaimCapacity`, set `WaitlistMode = true` and raise `WaitlistModeActivatedDomainEvent` after increment when `WaitlistEnabled && UsedCapacity >= MaxCapacity`
- [x] 1.4 Add `TicketCatalog.ReEvaluateWaitlistMode(ticketTypeId)` that clears `WaitlistMode` only when `UsedCapacity < MaxCapacity AND active entries == 0 AND issued coupons == 0`; call after coupon redemption, expiry cycle, and admin entry removal
- [x] 1.5 Expose `waitlistMode` and `waitlistEnabled` booleans in ticket type listing and detail API responses
- [x] 1.6 Domain tests: WaitlistMode activates on last slot when WaitlistEnabled; does not activate when WaitlistEnabled false; retroactive activation on sold-out type; force-disable clears WaitlistMode and entries regardless of capacity; capacity increase triggers ProcessWaitlistNotifications; removing capacity limit force-disables; mode lifts when all three conditions met; Integration tests: fill to capacity → verify waitlistMode true; enable on sold-out → WaitlistMode activates immediately; force-disable with entries and pending coupons → cleanup and cancellation emails; capacity increase with pending coupons in flight → WaitlistMode does NOT lift until coupons expire or are redeemed

---

## 2. Waitlist aggregate and core domain

- [x] 2.1 Create `Waitlist` aggregate root: `WaitlistId` (EventId + TicketTypeId), `Entries` collection (WaitlistEntryId GUID, Email, Position int, AddedAt UTC, Status Active/Removed), `WaitlistCoupons` collection (CouponId GUID, Status Issued/Redeemed/Revoked); add EF Core entity configuration and migration; add `WaitlistRepository`
- [x] 2.2 Add `WaitlistModeActivatedDomainEventHandler`: creates a new `Waitlist` aggregate for the ticket type in the same `RegistrationsDbContext` (dispatched synchronously in `SavingChangesAsync`, same transaction)
- [x] 2.3 Implement `JoinWaitlist { EventId, TicketTypeId, Email }` command and handler: validate WaitlistEnabled and WaitlistMode active; idempotent on duplicate email (return success, no new email); otherwise raise `WaitlistEntryAdded` triggering verification email via Email facade; HMAC-signed link valid 24 hours
- [x] 2.4 Implement `POST /events/{teamSlug}/{eventSlug}/waitlist/{ticketTypeId}/confirm?token={hmac}` public endpoint: validate HMAC token; create active waitlist entry at next position (idempotent)
- [x] 2.5 Implement `LeaveWaitlist { EventId, TicketTypeId, Email }` command and handler: remove active entry (idempotent if not found); raise `WaitlistEntryRemoved`; call `ReEvaluateWaitlistMode`
- [x] 2.6 Raise `WaitlistExhaustedDomainEvent` from `Waitlist` when `Entries.Count(Active) == 0 AND WaitlistCoupons.Count(Issued) == 0`; add `WaitlistExhaustedDomainEventHandler` that calls `TicketCatalog.TryDeactivateWaitlistMode(ticketTypeId)` (clears only when `UsedCapacity < MaxCapacity`); add `TicketCatalog.ForceDeactivateWaitlistMode(ticketTypeId)` for unconditional admin force-disable path
- [x] 2.7 Implement admin `DELETE /admin/teams/{teamId}/events/{eventId}/ticket-types/{ticketTypeId}/waitlist/{entryId}` endpoint
- [x] 2.8 Domain tests: join adds entry at correct position; idempotent join; cannot join when WaitlistMode false; leave removes entry and re-numbers positions; idempotent leave; `WaitlistExhaustedDomainEvent` fires when entries and issued coupons are both zero; event does NOT fire when issued coupons still outstanding; WaitlistCoupon transitions Issued→Redeemed and Issued→Revoked; `TryDeactivateWaitlistMode` stays active when at capacity; clears when capacity available and waitlist exhausted; Integration test: full join flow including email verification

---

## 3. WaitlistOnly mode enforcement in attendee registration

- [x] 3.1 Reject self-service registration with reason `"ticket type in waitlist mode"` when `WaitlistMode = true` and no coupon bypass; map to distinct rejection reason in response
- [x] 3.2 In the registration command handler, after successful coupon registration, check `coupon.Source == Waitlist`; if so, load `Waitlist` aggregate and call `waitlist.RedeemCoupon(couponId)` in the same transaction; organiser-provisioned coupons do not interact with the `Waitlist` aggregate
- [x] 3.3 Domain tests: self-service returns `"ticket type in waitlist mode"`; coupon-based registration succeeds when WaitlistMode active; Integration tests: self-service rejected with correct reason; redeeming a waitlist coupon marks WaitlistCoupon as Redeemed and may trigger WaitlistExhaustedDomainEvent; redeeming an organiser coupon does not affect the Waitlist aggregate

---

## 4. Coupon source discriminator and system-generated waitlist coupons

- [x] 4.1 Add `Source` enum (`Organiser`, `Waitlist`) to `Coupon` aggregate; `Waitlist` coupons suppress the invitation-email trigger; waitlist coupons cannot be created via the organiser API; update EF Core entity configuration and add migration
- [x] 4.2 Expose `source` field in coupon list and detail API responses
- [x] 4.3 Domain tests: organiser coupon triggers invitation email; waitlist coupon does not trigger invitation email; Integration test: coupon list returns correct `source` for each type

---

## 5. Waitlist notification flow

- [x] 5.1 Implement `ProcessWaitlistNotifications { EventId, TicketTypeId, FreedSlots }` command and handler: compute `ExpiresAt = max(utcNow, nextAllowedWindowStart) + ClaimWindowHours` in event timezone (using `WaitlistClaimWindowCalculator`); issue one waitlist coupon per freed slot; remove each notified entry from the waitlist; send notification email per attendee; call `ReEvaluateWaitlistMode`
- [x] 5.2 Trigger `ProcessWaitlistNotifications` from `RegistrationCancelled` domain event handler when ticket type has `WaitlistEnabled` and WaitlistMode is active
- [x] 5.3 Add `QuietHoursStart` and `QuietHoursEnd` (TimeOnly, defaults 22:00/08:00) to `TicketedEvent`; expose in event update endpoint; add `WaitlistClaimWindowCalculator.ComputeExpiresAt(utcNow, timeZone, quietHours, claimWindowHours)` helper
- [x] 5.4 Add `SendWaitlistNotificationAsync(email, couponCode, ticketTypeName, expiresAt)` to `IEventEmailFacade`; create email template with coupon code, expiry deadline, pre-filled registration link, and note about extended claim window when quiet hours apply
- [x] 5.5 Domain tests: correct `ExpiresAt` when issued outside quiet hours (`now + ClaimWindowHours`); correct `ExpiresAt` when issued during quiet hours (`nextAllowedWindowStart + ClaimWindowHours`, email sent immediately); Integration tests: cancel registration → waitlist notified → coupon with correct expiry → email sent; quiet hours produces extended expiry not delayed send; fewer entries than freed slots — only available entries notified and remaining capacity re-evaluated; capacity increase with in-flight coupons → WaitlistMode does NOT lift until coupons expire or are redeemed

---

## 6. Expiry processing and next-batch cascade

- [x] 6.1 Implement `ProcessExpiredWaitlistCouponsJob` (Quartz, polling every 5 minutes) in Worker host: query for waitlist coupons where `ExpiresAt <= utcNow - 2 minutes` (grace period to avoid racing with last-second redemptions); for each: call `waitlist.RevokeCoupon(couponId)`, fire `ProcessWaitlistNotifications` (1 freed slot), call `ReEvaluateWaitlistMode`
- [x] 6.2 Integration tests: coupon expires → next person on waitlist is notified; last coupon expires and waitlist empty → WaitlistMode is lifted; coupon whose `ExpiresAt` is within the 2-minute grace period is NOT processed by the job; Domain test: concurrent expiry-revoke and attendee-redemption of the same coupon — EF Core concurrency token ensures only one write succeeds; registration handler treats conflict as "coupon no longer valid"

---

## 7. Public coupon lookup endpoint

- [x] 7.1 Implement unauthenticated `GET /events/{teamSlug}/{eventSlug}/coupons/{couponCode}` returning `{ status, allowedTicketTypes: [{ id, name }], expiresAt }`; 404 when coupon does not exist for the given event; do not return target email
- [x] 7.2 Integration tests: active waitlist coupon returns correct status and ticket types; redeemed coupon returns `status: "redeemed"`; non-existent coupon code returns 404; coupon from different event returns 404

---

## 8. Admin UI — Ticket type form

- [x] 8.1 Add `waitlistEnabled` toggle (visible only when capacity is set) and `claimWindowHours` number input (visible only when enabled) to ticket type create/edit form; show confirmation dialog before disabling waitlist while WaitlistMode is active (warns that pending coupons will be revoked and entries removed); regenerate SDK

---

## 9. Admin UI — Event settings form

- [x] 9.1 Add `quietHoursStart` and `quietHoursEnd` time pickers (with help text explaining that quiet hours extend the claim window rather than delaying notification) to the existing event settings form alongside the existing timezone selector; regenerate SDK

---

## 10. Admin UI — Waitlist management page

- [x] 10.1 Implement waitlist management page at `/admin/teams/{teamId}/events/{eventId}/ticket-types/{ticketTypeId}/waitlist`: show active entries (ranked, masked email, join date), pending notifications (masked email, expiry countdown), summary stats (total waiting, total pending, sent today), and per-entry Remove button; use generated SDK functions in proxy routes

