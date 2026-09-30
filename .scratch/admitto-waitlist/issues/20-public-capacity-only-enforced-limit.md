# 20: Public capacity is the only enforced limit

**What to build:** A ticket type has one capacity, `PublicCapacity`: the seats available through self-service. Admin registrations, organiser coupons and VIP promotions are always on top of it and never use or free a public seat. This replaces the reserved-buffer model and removes the overbooking cases from the waitlist. See ADR-019.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

Today `MaxCapacity` is the total, and `ReservedCapacity` holds a buffer back from it for admin claims. Admin claims beyond the buffer spill into the public pool, and VIP offers hold a seat beyond capacity, so waitlist availability can go negative and has to be paid back (ticket 16). Admins can exceed any limit anyway, so the total mostly created bookkeeping.

### Capacity model

- [ ] `TicketType.MaxCapacity` is renamed to `PublicCapacity` (`int?`, `null` = no limit on self-service).
- [ ] `ReservedCapacity`, `ReservedUsedCapacity`, `HeldBack` and their validation errors (`ReservedCapacityNegative`, `ReservedCapacityRequiresBoundedCapacity`, `ReservedCapacityExceedsCapacity`) are removed.
- [ ] `UsedCapacity` is renamed to `PublicUsedCapacity`. A new `AdminUsedCount` counts admin tickets, for information only; nothing enforces it.
- [ ] `ClaimMode` becomes `Public` / `Admin`:
  - `Public` covers self-service claims (enforced as today) and redeeming an automatic waitlist offer (not enforced; converts the offer's hold, as `PublicUncapped` does now).
  - `Admin` covers admin registration, tickets added by an admin edit, organiser coupons and VIP offers. It is never enforced, increments `AdminUsedCount` only and never touches `WaitlistHeldCapacity`.
- [ ] `TicketCatalog.Release` credits the pool on the ticket's `TicketTypeSnapshot.Mode`. Releasing an admin ticket decrements `AdminUsedCount` and raises no `WaitlistCapacityAvailableDomainEvent`.
- [ ] Availability is `PublicCapacity − PublicUsedCapacity − WaitlistHeldCapacity`. Keep the unclamped value for waitlist decisions, and clamp it at 0 for public sales, `IsSoldOut`, the public status and waitlist activation. The only way it goes negative is lowering `PublicCapacity` below what's committed, which stays allowed.
- [ ] `WaitlistEnabled` still requires a bounded `PublicCapacity`. `SelfServiceEnabled` stays as the way to make a ticket type admin-only.
- [ ] Lowering `ReservedCapacity` no longer exists as a trigger for `WaitlistCapacityAvailableDomainEvent`. Raising `PublicCapacity` still triggers it.

### Waitlist

- [ ] A VIP offer (`Waitlist.IssueCouponToEntry`) takes no hold. Only automatic offers call `HoldForWaitlistOffer`, and only expired automatic offers release a hold. The invariant becomes: `WaitlistHeldCapacity` equals the number of `Issued` **automatic** coupons on the `Waitlist`.
- [ ] Redeeming a VIP coupon claims with `ClaimMode.Admin`. `Coupon.RedemptionClaimMode` needs to tell VIP from automatic waitlist coupons (for example by carrying the origin on the coupon; implementer's choice).
- [ ] `WaitlistQueuedCount` still goes down on VIP promotion, since the attendee leaves the queue.
- [ ] Deficit payback stays in the mechanism (offers go out only while availability > 0), but no test or doc should describe VIP or organiser overbooking as a cause anymore.

### Admin edits

- [ ] `ChangeAttendeeTicketsHandler` (admin mode) keeps the tickets it doesn't change in their original pool, claims the added ones with `ClaimMode.Admin` and releases the removed ones to the pool they came from.

### Persistence

- [ ] `ticket_types` JSON: `max_capacity` → `public_capacity`, `used_capacity` → `public_used_capacity`, `reserved_used_capacity` → `admin_used_count`, and `reserved_capacity` is dropped. Registration ticket `mode` values become `Public` / `Admin`.
- [ ] There's no live data (ticket 18's pre-deploy check confirms this). If that check finds any, the migration maps `Reserved` → `Admin` and `PublicUncapped` → `Public`, and renames the JSON keys in place.

### API, SDK and admin UI

- [ ] The add/update ticket type requests, validators and `TicketTypeDto` expose `publicCapacity` and drop `reservedCapacity`. The DTO exposes `publicUsedCapacity` and `adminUsedCount`. The public ticket-types API is unchanged apart from the availability it reports.
- [ ] Regenerate the admin UI SDK.
- [ ] The add and edit ticket type forms label the field "Public capacity", with help text saying that admin registrations and coupons come on top. The reserved capacity field is removed.
- [ ] The ticket types page, ticket breakdown card and event hero card show `public X/Y · admin Z · total N`. No warning is shown when the total exceeds anything, because there's no total to exceed.
- [ ] Vitest coverage for the forms and the display.

### Tests

- [ ] Domain tests: admin claims never change public availability; releasing an admin ticket frees no public seat; lowering `PublicCapacity` below used leaves availability negative and public sales sold out; a VIP offer takes no hold.
- [ ] Integration tests:
  - An admin registration on a sold-out ticket type succeeds, and public availability is unchanged.
  - Cancelling an admin registration in WaitlistMode issues no offer. Cancelling a public registration issues one.
  - VIP promoted while sold out: no hold is taken and no other offer goes out. Redeemed, it's an admin ticket. Cancelled later, it produces no offer.
  - A VIP offer that lapses produces no offer.
  - Lowering `PublicCapacity` by N, then N+1 cancellations: exactly one offer.
  - An admin edit that adds a ticket to a public registration keeps the existing ticket as public and adds an admin ticket.
- [ ] Remove or rewrite the ticket 16 tests that relied on VIP or organiser overbooking ("VIP at position 2…", "VIP redeemed (capacity Max + 1)…", "organiser coupon claimed past the reserved buffer…", "raising MaxCapacity by N with outstanding VIP offers…").
- [ ] The held-capacity invariant test covers automatic offers only, and asserts that a VIP issue, expiry and redemption leave `WaitlistHeldCapacity` unchanged.
- [ ] Persistence tests use the new JSON keys.

### Docs

- [ ] arc42 §8.14: replace "Reserved capacity as a sales restriction, not a separate pool" with the public-capacity model and link ADR-019. The waitlist-held capacity section covers automatic offers only, and VIP offers are described as admin tickets.
- [ ] arc42 §6: the capacity-change, cancellation and VIP flows match.
- [ ] CONTEXT.md: glossary entries for **Public capacity** (_Avoid_: max capacity, total capacity) and **Admin ticket** (a ticket claimed by an admin action, on top of public capacity; _Avoid_: reserved ticket).
