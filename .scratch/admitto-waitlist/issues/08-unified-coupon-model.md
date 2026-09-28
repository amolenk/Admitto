# 08: Unified coupon model

**What to build:** Organizer-issued and waitlist-issued coupons stop being two behaviorally different mechanisms with duplicated, inconsistent rules. There is one coupon behavior; the source only decides which email fires at creation. Redemption also learns to handle a coupon covering more than one ticket type, and consistently cleans up any matching waitlist entry regardless of how the coupon originated.

**Blocked by:** 04 (Update-registration supports confirmed⟷waitlist switching)

**Status:** ready-for-agent

- [ ] Coupon `Source` (Organiser / Waitlist) no longer gates which redemption code paths are permitted; a coupon of either source can be redeemed through the self-service update-registration endpoint.
- [ ] Coupon redemption accepts a coupon whose allowed ticket types include more than one ticket type.
- [ ] Redemption succeeds as long as the final ticket selection overlaps the coupon's allowed ticket types by at least one; ticket types on the coupon not included in the final selection are forfeited, and the coupon is still marked fully redeemed (single-use, as today).
- [ ] At redemption time, for every ticket type actually granted by that redemption, any active waitlist entry belonging to the redeeming email for that ticket type is removed — regardless of the coupon's source.
- [ ] This redemption-time waitlist cleanup is exercised for both the create-with-coupon path and the update-with-coupon path.
- [ ] Domain tests cover Coupon's generalized, partial-tolerant, multi-ticket-type redemption rule in isolation.
- [ ] Integration tests cover: redeeming an Organiser-sourced multi-ticket-type coupon via update, redeeming a single-ticket-type Waitlist-sourced coupon via update (regression), and waitlist-entry cleanup firing correctly in both cases.
