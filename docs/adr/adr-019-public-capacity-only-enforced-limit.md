# ADR-019: Public capacity is the only enforced limit; admin claims are additive

## Status
Accepted. Supersedes the "Reserved capacity as a sales restriction, not a separate pool" design in arc42 §8.14.

## Context
A ticket type had a `MaxCapacity` (the total) and a `ReservedCapacity` (a buffer held back from self-service for admin registrations and organiser coupons). To let admin and public claims arrive in any order, the catalog tracked `ReservedUsedCapacity` and derived `HeldBack = max(0, ReservedCapacity − ReservedUsedCapacity)`. Admin claims beyond the buffer spilled into the public pool, and VIP waitlist offers could hold a seat beyond capacity. As a result the availability that drives waitlist offers could go negative, and a freed seat first paid back the overbooking before anyone in the queue got an offer (ticket 16).

Admins are not bound by capacity or registration windows anyway: `ClaimMode.Reserved` never rejects, and `ReservedCapacity` was a floor, not a ceiling. Most of the bookkeeping existed to keep a total that admins were free to exceed.

## Decision
A ticket type has one enforced number, `PublicCapacity`: the seats available through self-service. Self-service claims and automatic waitlist offers count against it. Everything an admin does is on top of it and never uses or frees a public seat. That covers admin registrations, admin-added tickets, organiser coupons and VIP promotions.

- `ReservedCapacity`, `ReservedUsedCapacity` and `HeldBack` are removed, and so is the spill of admin claims into the public pool.
- `TicketType` counts `PublicUsedCapacity` (enforced) and `AdminUsedCount` (for information only), alongside the existing `WaitlistHeldCapacity` and `WaitlistQueuedCount`.
- Availability is `PublicCapacity − PublicUsedCapacity − WaitlistHeldCapacity`. The only way it can go negative is an organiser lowering `PublicCapacity` below what's committed, and cancellations make up that shortfall before the queue gets an offer.
- A ticket records the pool it came from (`ClaimMode.Public` / `ClaimMode.Admin`), and a release credits that pool only.
- A VIP offer takes no public hold. When it's redeemed, the ticket is an admin ticket.
- An admin registering someone who holds an automatic offer creates an admin ticket. The offer expires and its hold goes to the next person in the queue.
- When an admin edits a registration's tickets, kept tickets stay in their original pool, added tickets are admin tickets, and removed tickets are released to the pool they came from.
- `SelfServiceEnabled` remains the way to make a ticket type admin-only. `PublicCapacity = 0` just means sold out. `PublicCapacity = null` means no limit on self-service, and a waitlist still requires a bounded capacity.

## Consequences
- The system no longer knows a physical venue total. Keeping attendance within the venue is the organiser's job, just as closing dates already are for admins. The admin UI shows `public X/Y · admin Z · total N` so organisers can see it.
- Admins don't have to declare a buffer up front. They can register or invite as many people as they like without affecting public sales.
- The waitlist logic loses its overbooking cases: VIP deficits and organiser overbooking no longer exist, so the "pay back before the queue" rule only applies after an organiser lowers capacity.
- A VIP attendee who cancels frees no public seat, so nobody in the queue gets an offer from that cancellation.

## References
- arc42 §8.14 — capacity and waitlist-held capacity.
- `.scratch/admitto-waitlist/issues/20-public-capacity-only-enforced-limit.md` — the rework.
- Tickets 16 and 17 — waitlist-held capacity and queued count, which remain.
