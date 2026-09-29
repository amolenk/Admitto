# 16: Waitlist offers hold catalog capacity

**What to build:** How many waitlist offers go out is decided by the `TicketCatalog` from real capacity, not by counting "freed slot" events. Every outstanding waitlist offer — automatic or VIP — holds a seat on its ticket type. A VIP offer made while sold out is covered by the next seat that frees up instead of that seat going to the queue, and any overbooking (a redeemed VIP, or an organiser coupon claimed past the reserved buffer) is paid back before anyone else in the queue gets an offer.

**Blocked by:** 13 (Expired VIP coupon must not cascade)

**Status:** ready-for-agent

Ticket 13 stops a lapsed VIP coupon from cascading, but a VIP stays a permanent extra seat: `RegistrationCancelledDomainEventHandler` cascades every cancelled ticket without looking at `UsedCapacity` versus `MaxCapacity`, so once a VIP redeems, capacity stays one over for good. Counting freed-slot events can't fix this. Deciding from shared counts instead needs the rule *used + outstanding offers ≤ capacity* to live in one aggregate, because today it spans two — the catalog owns `UsedCapacity`, the `Waitlist` owns the outstanding offers — and a transaction that reads one and writes the other isn't protected by `xmin` (e.g. the expiry job and a concurrent organiser registration or cancellation can each commit on stale counts, sending an offer too many or too few).

**Capacity held by offers**
- [ ] `TicketType` gets `WaitlistHeldCapacity` (int, stored in the `ticket_types` JSON with the other counters; a missing key reads as 0 — no live data, so no backfill).
- [ ] `TicketCatalog` gets `HoldForWaitlistOffer(ticketTypeId)`, `ReleaseWaitlistHold(ticketTypeId)` and a redemption path that turns a hold into a `ClaimMode.PublicUncapped` claim (hold −1, `UsedCapacity` +1). Taking a hold is always allowed, even with no seats available (a VIP offer goes over).
- [ ] Every waitlist offer takes a hold in the same unit of work as the `WaitlistCoupon` is created: `Waitlist.IssueNextCoupon` (automatic) and `Waitlist.IssueCouponToEntry` (VIP).
- [ ] An expired waitlist offer releases its hold (`ProcessExpiredWaitlistCouponsJob`, and the no-ticket-type branch).
- [ ] Redeeming a waitlist coupon (`RegisterAttendeeWithCouponHandler`, `ChangeAttendeeTicketsHandler`, `UpdatePartnerRegistrationHandler` — every caller of `Coupon.RedemptionClaimMode` with a waitlist coupon) converts its hold instead of claiming on top of it.
- [ ] Invariant: for every ticket type, `WaitlistHeldCapacity` equals the number of `Issued` coupons on its `Waitlist`. An integration test asserts it after issue (automatic and VIP), expiry and redemption.

**Availability**
- [ ] An unclamped internal availability `MaxCapacity − UsedCapacity − HeldBack − WaitlistHeldCapacity` drives waitlist decisions; it may go negative (outstanding VIP offers, organiser overbooking).
- [ ] `PublicAvailableCapacity` also subtracts `WaitlistHeldCapacity` and stays clamped at 0; `IsSoldOut`, the public ticket-type status and waitlist activation keep using it.

**Offering seats**
- [ ] While a ticket type is in `WaitlistMode`, any catalog change that leaves the unclamped availability > 0 raises `WaitlistCapacityAvailableDomainEvent(ticketTypeId, n)` with n = that availability: releasing tickets, releasing a hold, raising `MaxCapacity`, lowering `ReservedCapacity`. This replaces `WaitlistCapacityFreedDomainEvent`.
- [ ] Its handler runs in the same transaction (a domain event handler, as today) and issues up to `min(n, active entries)` coupons via `IssueNextCoupon`, each taking a hold.
- [ ] The NotifyWaitlist `RegistrationCancelledDomainEventHandler` is removed; the cancellation's `Release` raises the event instead. `ProcessWaitlistNotificationsCommand.FreedSlots` is removed (the command either disappears into the new handler or becomes a plain "re-evaluate" command — implementer's choice).
- [ ] The expiry job no longer counts freed slots: it expires each coupon and releases its hold, and the catalog decides whether a seat is available. The `bool` returned by `Waitlist.RevokeCoupon`/`ExpireCoupon` (ticket 13) is dropped. `WaitlistCouponOrigin` stays, for history and display only.
- [ ] Verify that domain events raised *inside* a domain event handler get dispatched in the same `SaveChanges`. `DomainEventsInterceptor` snapshots the change-tracker entries and clears each provider's events after dispatching, so events added during dispatch — or on entities first loaded by a handler — may be lost. If so, make it loop until no events are pending, and cover that with a test.

**WaitlistMode**
- [ ] `Waitlist.IssuedCouponCount` is removed. WaitlistMode decisions (`ReEvaluateWaitlistMode`, the forced-deactivation check) use the catalog's `WaitlistHeldCapacity` for outstanding offers. `Waitlist.CheckExhausted` keeps using its own coupon records.
- [ ] Waitlist history (removed entries, redeemed/expired coupons) is kept, not cleared, when the waitlist is exhausted.

**Tests** (integration, unless noted)
- [ ] VIP promoted at position 1 while sold out: a hold is taken, no other offer goes out.
- [ ] VIP at position 2, and #1's automatic offer lapses while the VIP offer is outstanding: no offer goes to #3 (the freed seat now covers the VIP).
- [ ] VIP offer lapses while sold out: no new offer.
- [ ] VIP redeemed (capacity Max + 1), then a registration is cancelled: no offer; capacity back at Max. A second cancellation: one offer.
- [ ] An organiser coupon claimed past the reserved buffer, then a cancellation: no offer.
- [ ] Raising `MaxCapacity` by N with outstanding VIP offers: offers equal to N minus the VIP holds (never below 0).
- [ ] Concurrency: the expiry job and a concurrent catalog write on the same ticket type (e.g. an organiser registration) cannot both commit — one hits a `DbUpdateConcurrencyException`.
- [ ] Domain tests for the hold operations, the unclamped vs clamped availability, and when `WaitlistCapacityAvailableDomainEvent` is raised.

**Docs**
- [ ] arc42 §8.14: describe waitlist-held capacity and the unclamped availability; fix the `ClaimMode.PublicUncapped` sentence ("the coupon already redeemed a slot freed from the public pool when it was issued"), which is wrong for VIP coupons — it now converts the offer's hold.
- [ ] arc42 §6: the cancellation / expiry / capacity-raise runtime flows go through `WaitlistCapacityAvailableDomainEvent`.
- [ ] arc42 §8.9: a short note that a decision read from one aggregate and written to another is not protected by `xmin`, so the count a decision depends on belongs in the aggregate that makes it.
- [ ] arc42 §11: add a tech-debt row for WaitlistMode being lifted based on the `Waitlist`'s queue length, which isn't protected either (fixed by ticket 17).

**Note:** tickets 14 and 15 also change this area. 14 renames statuses and folds `RevokeCoupon` into `ExpireCoupon` — whichever lands second rebases the job and the release-hold call. 15's "handler for `WaitlistCapacityFreedDomainEvent`" becomes the `WaitlistCapacityAvailableDomainEvent` handler here; if 15 lands first, this ticket replaces its handler. With explicit disable (15), outstanding offers keep their hold until they expire; with the capacity limit removed (15), holds no longer affect availability because capacity is unbounded.
