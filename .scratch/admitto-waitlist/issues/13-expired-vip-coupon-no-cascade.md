# 13: Expired VIP coupon must not cascade to the next attendee

**What to build:** When a manually promoted (VIP) waitlist coupon lapses unclaimed, the expired-coupon job stops treating it as a freed slot, so no extra offer goes out to the next person in the queue and capacity isn't exceeded further.

**Blocked by:** 11 (Manual VIP waitlist promotion, backend)

**Status:** ready-for-agent

Today a VIP coupon is not backed by a freed slot, but `ProcessExpiredWaitlistCouponsJob` counts every expired waitlist coupon as one and fires `ProcessWaitlistNotificationsCommand` for it. The resulting offer is redeemed as `ClaimMode.PublicUncapped`, so each lapsed VIP coupon can push `UsedCapacity` one further past `MaxCapacity`. See the tech-debt entry in `docs/arc42/11-risks-and-technical-debt.md` and §8.14 in `docs/arc42/08-crosscutting-concepts.md`.

A "VIP coupon" here means only a coupon issued by `Waitlist.IssueCouponToEntry` (the "Promote to VIP" action on an active waitlist entry). Organiser-issued coupons are never tracked on a `Waitlist` and are unaffected.

- [ ] `WaitlistCoupon` gets an origin: a `WaitlistCouponOrigin { Automatic, Manual }` enum, with `Automatic` as the first (default) value, stored as text like other enums. The flag lives on `WaitlistCoupon`, not on `Coupon` — `Coupon` stays source-agnostic (unified coupon model).
- [ ] `Waitlist.IssueCouponToEntry` marks its coupon `Manual`; `IssueNextCoupon` marks it `Automatic`.
- [ ] An EF Core migration is added to keep the model snapshot in sync. `WaitlistCoupon` is an owned JSON collection, so no SQL backfill: existing rows without the property read back as `Automatic`. An integration test loads a `Waitlist` whose coupon JSON lacks the origin key and asserts it reads as `Automatic`.
- [ ] When a manual coupon expires, the job still expires it on the waitlist and sends the expired-offer email, but it does not count it towards the number of slots to cascade. The job resolves each expired `Coupon` to its `WaitlistCoupon` (the waitlist is already loaded with its coupons) to read the origin.
- [ ] A group of expired coupons that mixes manual and automatic ones cascades only the automatic count.
- [ ] The job always calls `ProcessWaitlistNotificationsCommand`, even when the automatic count is 0, so `WaitlistMode` re-evaluation / forced deactivation still runs.
- [ ] Outstanding VIP coupons keep counting in `Waitlist.IssuedCouponCount` (no change): they keep `WaitlistMode` on and delay `WaitlistExhaustedDomainEvent` until they are redeemed or expire.
- [ ] The unused `Waitlist.TrackIssuedCoupon` is deleted.
- [ ] Integration tests for `ProcessExpiredWaitlistCouponsJob` cover: an expired VIP coupon (no new offer, expired-offer email still sent), a mixed group (offers equal to the automatic count), and an expired VIP coupon with an empty queue and no other outstanding coupons (`WaitlistMode` lifted).
- [ ] Domain tests cover the origin set by `IssueNextCoupon` vs `IssueCouponToEntry`.
- [ ] The tech-debt row in `docs/arc42/11-risks-and-technical-debt.md` is removed, and the §8.14 / §6 runtime-view wording is updated.

**Note:** ticket 14 also changes `WaitlistCoupon` (adds `ExpiresAt`, renames statuses) and the expiry job. The two are independent, but whichever lands second must rebase onto the other's job changes and migration.
