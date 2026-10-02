# ADR-020: Unify registration creation into a single partner API endpoint

## Status
Accepted.

## Context
Partners created registrations through two separate endpoints: `POST /registrations` for self-service (available + waitlisted tickets, no coupon) and `POST /registrations/coupon` (coupon-only, requiring every selected ticket type to be allow-listed by the coupon via `EnsureAllowsAll`). The update endpoint (`PUT /registrations/{id}`), in contrast, already accepted available tickets, waitlisted tickets, and an optional coupon in one request, redeeming the coupon only for the newly confirmed tickets it allow-lists while classifying the rest through normal public capacity/waitlist rules. This asymmetry meant a partner could combine coupon and non-coupon tickets in a single update but not in a single create, and the two create handlers duplicated classification logic that was already drifting from update's.

## Decision
Replace both create endpoints with one: `POST /api/events/{eventSlug}/registrations`, backed by `RegisterAttendeeCommand`/`RegisterAttendeeHandler`. The request carries `RegisterTicketTypeIds`, `WaitlistTicketTypeIds`, and an optional `CouponCode`, matching update's shape. A coupon only needs to allow-list the subset of selected ticket types it covers; other selected ticket types go through normal public capacity/waitlist classification in the same request, and `Coupon.BypassRegistrationWindow` bypasses the registration window only for its own granted ticket types. The request is atomic: any ticket type that is neither available/waitlistable nor coupon-covered fails the whole request. The per-ticket-type classification logic is extracted into a pure domain service (`Registrations/Domain/Services`) shared by both the create and update handlers, replacing the duplicated logic that previously lived only in `UpdatePartnerRegistrationHandler`.

This is a breaking change to the partner API: `RegisterAttendeeSelfServiceCommand`/`Handler`, `RegisterAttendeeWithCouponCommand`/`Handler`, and the `/registrations/coupon` route are deleted rather than deprecated. No internal caller (the Admin UI has no proxy route for either endpoint) needed migration at the time of this decision.

## Consequences
- Partners integrating against the old two-endpoint contract must migrate to the single endpoint; there is no compatibility shim.
- Create and update now share one classification path, so future changes to coupon/waitlist/capacity rules only need to be made once.
- The coupon-only invariant `EnsureAllowsAll` (every selected ticket type must be allow-listed) no longer applies to creation; a coupon may now cover a strict subset of a request's ticket types, consistent with update's existing behavior.

## References
- `docs/arc42/06-runtime-view.md` — registration creation/update flow.
- `docs/arc42/08-crosscutting-concepts.md` §8.14 area — capacity, waitlist, and coupon conventions.
</content>
