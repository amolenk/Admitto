# 14: Remove coupon revoke functionality

**What to build:** There is no way to revoke a coupon any more — neither organiser-issued nor waitlist-issued. Coupons simply end by being redeemed or by expiring. The "revoked" concept disappears from the domain and from storage; where the system currently uses "revoke" to mean "this waitlist offer lapsed", it uses expiry terms instead.

**Blocked by:** none

**Status:** ready-for-agent

Today the admin revoke endpoint (`POST .../coupons/{couponId}/revoke`) only sets `Coupon.RevokedAt`; for a waitlist coupon it never touches the matching `WaitlistCoupon`, which then stays `Issued` forever — the slot never cascades and the waitlist can never become exhausted. The endpoint has no hand-written Admin UI caller. Internally, `ProcessExpiredWaitlistCouponsJob` also uses revoke (`Coupon.Revoke()`, `Waitlist.RevokeCoupon`, `WaitlistCouponStatus.Revoked`) to record that it processed a lapsed coupon. There is no live data, so stored names may change freely.

- [ ] The admin revoke-coupon endpoint, `RevokeCouponCommand`, `RevokeCouponHandler` and their tests are removed, and `MapRevokeCoupon()` is removed from `RegistrationsModule`.
- [ ] The Admin UI SDK is regenerated so it no longer exports the revoke client function.
- [ ] `Coupon.Revoke()`, `Coupon.RevokedAt` and `CouponStatus.Revoked` are removed. A coupon's status is Active / Redeemed / Expired, derived from `RedeemedAt` and `ExpiresAt`. `Coupon`'s redemption rules drop the revoked check. `GetCouponDetails` and its DTO drop `RevokedAt`.
- [ ] `WaitlistCoupon` gets an `ExpiresAt` (copied from the coupon at issuance), and `WaitlistCouponStatus` becomes `Issued / Redeemed / Expired`.
- [ ] `Waitlist.RevokeCoupon` is removed; its logic is folded into `Waitlist.ExpireCoupon`, which also handles the "ticket type no longer exists" case inside the aggregate (expire without raising the expired-offer email).
- [ ] `ProcessExpiredWaitlistCouponsJob` finds its work via waitlists holding `Issued` coupons whose `ExpiresAt` is past the cutoff (grace period unchanged), rather than via `Coupon.RevokedAt == null`. It loads the matching `Coupon` only for what the expired-offer email needs. If EF cannot translate the query into the owned JSON collection, fall back to a `Coupon.LapsedAt` marker set by the job (and raise it in review).
- [ ] An EF Core migration drops the `RevokedAt` column and reflects the `WaitlistCoupon` changes. No data migration is needed (no live data).
- [ ] Existing domain/integration tests for coupons, `Waitlist` and the expiry job are updated; no references to revoke remain in the Registrations module.
- [ ] Docs (arc42 §6, §8, glossary if applicable) no longer mention coupon revocation; lapsed waitlist coupons are described as expired.

**Out of scope:** the "Disable waitlist?" dialog wording — rewritten in ticket 15.

**Note:** ticket 13 also changes `WaitlistCoupon` (adds an origin) and the expiry job. The two are independent, but whichever lands second must rebase onto the other's job changes and migration.
