# 11: Manual VIP waitlist promotion (backend)

**What to build:** An organizer can give a specific, already-waitlisted attendee priority access — issuing them a coupon out of normal first-in-line order — for VIP scenarios, without needing to look up and manually re-enter their email address.

**Blocked by:** 02 (Fix broken automatic promotion email), 08 (Unified coupon model)

**Status:** ready-for-agent

- [ ] A new admin capability allows issuing a coupon to one specific, named active waitlist entry, independent of that entry's queue position.
- [ ] Issuing this coupon uses the same claim-window/quiet-hours expiry calculation already used for automatic front-of-queue promotion.
- [ ] The targeted entry is removed from the active waitlist queue immediately upon issuance (not upon redemption), and remaining active entries' positions are renumbered.
- [ ] The issued coupon is Waitlist-sourced, and raises the same event that triggers the existing waitlist-offer email to the promoted attendee.
- [ ] Attempting to target an entry that is no longer active (already removed, already promoted, or never existed) is rejected with a clear error.
- [ ] Integration tests cover: issuing to a specific mid-queue entry (not the front of the queue), queue renumbering afterward, redeeming the resulting coupon through the update-registration endpoint, and the promotion email firing.
- [ ] An API-level test confirms the new admin route's authorization and request/response contract.
