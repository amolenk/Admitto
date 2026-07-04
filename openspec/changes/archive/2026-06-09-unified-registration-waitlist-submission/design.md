## Context

Public attendee registration currently treats the submitted ticket list as one registration selection. If any selected ticket type is in WaitlistOnly mode, the registration fails. Public waitlist joins are a separate endpoint per ticket type. This makes partner websites orchestrate multiple calls and makes mixed outcomes difficult to present consistently.

Waitlist coupons currently behave like registration coupons. That is a poor fit when the attendee already has an active registration for the event: claiming a waitlist offer should update the existing registration's tickets when necessary, not fail because the attendee is already registered.

Relevant constraints from the architecture docs:
- The Registrations module owns `TicketedEvent`, `TicketCatalog`, `Coupon`, `Registration`, and `Waitlist` state.
- Public write endpoints own the Registrations unit-of-work commit; handlers must not commit.
- `TicketCatalog` remains the atomic status + capacity gate for registration claims.
- Waitlist entries are per ticket type and waitlist coupon redemption must remain in one Registrations transaction with any registration/ticket changes.

## Goals / Non-Goals

**Goals:**
- Let a verified attendee submit one explicit public request that creates a registration for available tickets and joins selected waitlists in a single transaction.
- Keep the request deterministic by separating tickets to register from waitlists to join.
- Reject stale ticket-state expectations rather than silently reclassifying tickets between registration and waitlist actions.
- Allow waitlist intents to overlap with current registered tickets and with each other.
- Treat waitlist coupons as capacity grants that can be applied during self-service ticket change.
- Fix the existing waitlist coupon bug for attendees who already have an active registration.

**Non-Goals:**
- Finalize partner website UX details.
- Introduce a new waitlist-offer endpoint family.
- Add ranking or preference semantics across multiple waitlist entries.
- Allow persisted registrations to contain overlapping ticket types.
- Change admin registration or admin ticket-change behavior.

## Decisions

### D1: Reuse existing public registration and ticket-change endpoints

The public `POST /registrations` endpoint will be extended instead of adding a new combined submission endpoint. The public self-service ticket-change endpoint will be extended with optional waitlist coupon data instead of adding a separate waitlist claim endpoint.

Alternatives considered:
- New `registration-submissions` endpoint: clean naming, but duplicates the existing public registration action and increases partner API surface.
- Dedicated waitlist-offer claim endpoint: useful later for richer offer preview, but unnecessary if the existing ticket-change endpoint already accepts a final intended ticket set.

### D2: Use explicit register and waitlist ticket sets

The registration request should carry separate `registerTicketTypeIds` and `waitlistTicketTypeIds`. The server SHALL apply exactly those requested actions or apply none.

If a ticket in `waitlistTicketTypeIds` has left WaitlistMode by submission time, the server rejects the whole request with a stale-state conflict. It does not automatically register that ticket, because joining a waitlist is not the same commitment as holding the ticket.

Alternatives considered:
- Server best-effort classification from one `ticketTypeIds` list: smoother when state changes, but it can surprise users and makes stale-state handling less explicit.
- Client-side orchestration of separate registration and waitlist calls: simpler backend but cannot provide one transaction.

### D3: Enforce overlap only for actual registration ticket sets

`registerTicketTypeIds` must be a valid registration selection, including no overlapping time slots. `waitlistTicketTypeIds` are independent per-ticket intents and may overlap with registered tickets or other waitlist entries.

The final registered ticket set is validated when a waitlist coupon is claimed.

Alternatives considered:
- Reject overlaps between registered and waitlisted tickets: too restrictive because users often register for a backup workshop while waitlisting for their preferred workshop.
- Reject overlaps within the waitlist set: unnecessary because each waitlist entry has its own lifecycle and separate claim email.

### D4: Treat waitlist coupons as capacity grants

A waitlist coupon grants authority to claim one offered ticket type. During ticket change, the final requested ticket set must include that offered ticket type. The offered ticket bypasses capacity and WaitlistMode checks; all other newly claimed tickets use normal self-service enforcement.

If the attendee has an active registration, claiming the waitlist coupon changes that registration. If no active registration exists, the coupon-backed registration creation path remains available.

Alternatives considered:
- Keep waitlist coupon redemption as registration creation only: preserves existing flow but fails the common mixed registration + waitlist case.
- Auto-replace overlapping tickets without a final ticket set: simpler claim request, but assumes which current ticket the attendee wants to drop.

### D5: Preserve one transaction per public write

Registration creation will create the registration, claim capacity, and add any waitlist entries before the endpoint commits once. Ticket change with a waitlist coupon will change tickets, redeem the coupon, and mark the waitlist coupon redeemed before the endpoint commits once.

This follows the existing endpoint-owned unit-of-work rule and avoids partially persisted mixed outcomes.

## Risks / Trade-offs

- **Breaking public request shape** -> Coordinate partner website changes and regenerate API clients before UI/proxy code uses the new contract.
- **More complex handler logic** -> Keep registration creation and waitlist action validation explicit, with targeted tests for stale-state branches.
- **Race after validation** -> Continue relying on `TicketCatalog` optimistic concurrency and claim-time checks; stale-state errors should be reported when observed before commit, and concurrency conflicts remain possible.
- **Waitlist-only submissions create no registration** -> Response must clearly carry `registrationId: null` and waitlisted ticket IDs so partner sites can render the correct outcome.
- **Coupon ticket-change semantics can be misunderstood** -> Specify that the coupon grants capacity only for the offered ticket type and does not bypass final registration overlap validation.
