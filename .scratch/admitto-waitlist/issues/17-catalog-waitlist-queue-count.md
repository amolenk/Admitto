# 17: WaitlistMode lift can't race a waitlist join

**What to build:** Someone joining the waitlist at the same moment as WaitlistMode is being lifted can no longer end up queued with WaitlistMode off, where no cancellation would ever produce an offer for them.

**Blocked by:** 16 (Waitlist offers hold catalog capacity)

**Status:** ready-for-agent

`TicketCatalog` lifts WaitlistMode when there are no active entries, no outstanding offers and seats are available. After ticket 16 the outstanding offers are a catalog count, but "no active entries" is still read from the `Waitlist`. Joining the queue (`Waitlist.AddEntry`, from `RegisterAttendeeSelfServiceHandler` and `UpdatePartnerRegistrationHandler`) writes only the `Waitlist`, so a join and a mode lift running at the same time can both commit. The fix follows 16: the catalog tracks the count it makes the decision on.

- [ ] `TicketType` gets `WaitlistQueuedCount` (int, stored in the `ticket_types` JSON; a missing key reads as 0 — no live data).
- [ ] Every change to the number of active entries updates it in the same unit of work: `AddEntry` (join), `RemoveEntry` (both overloads), entry removal on coupon issuance (`IssueNextCoupon`, `IssueCouponToEntry`), entry removal on redemption (`ApplyCouponRedemption`), withdrawal on cancellation, and removing all entries on an explicit disable (ticket 15, if landed).
- [ ] WaitlistMode lift decisions (`ReEvaluateWaitlistMode`, the forced-deactivation check, the `WaitlistExhaustedDomainEvent` handler) read `WaitlistQueuedCount` from the catalog instead of a count passed in from the `Waitlist`.
- [ ] Invariant: for every ticket type, `WaitlistQueuedCount` equals the `Waitlist`'s active entry count. An integration test asserts it after join, leave, promotion (automatic and VIP), redemption and cancellation.
- [ ] Concurrency integration test: a waitlist join and a WaitlistMode lift on the same ticket type cannot both commit.
- [ ] The tech-debt row added by ticket 16 in arc42 §11 is removed; §8.14 lists the queued count next to waitlist-held capacity.
