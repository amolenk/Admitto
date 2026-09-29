# 13: Expired VIP coupon must not cascade to the next attendee

**What to build:** When a manually promoted (VIP) waitlist coupon lapses unclaimed, the expired-coupon job stops treating it as a freed slot, so no extra offer goes out to the next person in the queue and capacity isn't exceeded further.

**Blocked by:** 11 (Manual VIP waitlist promotion, backend)

**Status:** needs-triage

Today a VIP coupon is not backed by a freed slot, but `ProcessExpiredWaitlistCouponsJob` counts every expired waitlist coupon as one and fires `ProcessWaitlistNotificationsCommand` for it. The resulting offer is redeemed as `ClaimMode.PublicUncapped`, so each lapsed VIP coupon can push `UsedCapacity` one further past `MaxCapacity`. See the tech-debt entry in `docs/arc42/11-risks-and-technical-debt.md` and §8.14 in `docs/arc42/08-crosscutting-concepts.md`.

- [ ] The waitlist records whether each of its coupons was issued manually (VIP) or by automatic promotion; existing rows default to automatic (EF Core migration).
- [ ] `Waitlist.IssueCouponToEntry` marks its coupon as manual; `IssueNextCoupon` keeps marking coupons as automatic.
- [ ] When a manual coupon expires, the job still revokes it and sends the expired-offer email, but it does not count it towards the number of slots to cascade.
- [ ] A group of expired coupons that mixes manual and automatic ones cascades only the automatic count.
- [ ] If the waitlist is empty afterwards, `WaitlistExhaustedDomainEvent` behaviour is unchanged.
- [ ] Integration tests for `ProcessExpiredWaitlistCouponsJob` cover an expired VIP coupon (no new offer) and a mixed group (offers equal to the automatic count).
- [ ] The tech-debt row in `docs/arc42/11-risks-and-technical-debt.md` is removed, and the §8.14 / runtime-view wording is updated.
