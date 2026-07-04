## Context

The Registrations module is partially scaffolded. The `Coupon` aggregate and all coupon
management use cases (create, list, get, revoke) are fully implemented. The `Registration`,
`EventCapacity`, and `TicketCapacity` domain types exist but are commented out, and the
registration endpoints are stubs. No `EventRegistrationPolicy` concept exists yet.

The Organization module exposes event and ticket type data via `IOrganizationFacade`, but
`TicketTypeDto` only carries `Slug`, `Name`, and `IsCancelled` — time slots and capacity
are not yet surfaced. The Organization module also does not publish any module events for
ticket type lifecycle changes.

The `TicketGrantMode` enum (`SelfService` / `Privileged`) and the `IsSelfService` /
`IsSelfServiceAvailable` flags on `TicketType` were designed for an admin registration path
that is no longer part of this design. They will be removed.

The coupons migration `ReplaceTicketTypeIdsWithSlugs` established **slug** as the canonical
ticket type identifier in the Registrations module. The commented-out `EventCapacity` /
`TicketCapacity` scaffolding still uses `TicketTypeId` (UUID) — an inconsistency to resolve.

---

## Goals / Non-Goals

**Goals:**
- Implement self-service attendee registration with capacity, window, and domain enforcement
- Implement coupon-based registration that bypasses those rules (per coupon configuration)
- Introduce `EventRegistrationPolicy` as a new aggregate in the Registrations module to store per-event registration window and allowed email domain
- Synchronize per-ticket-type capacity into the Registrations module via module events from the Organization module
- Extend `IOrganizationFacade` / `TicketTypeDto` to expose time slots and capacity
- Remove obsolete `IsSelfService`, `IsSelfServiceAvailable` (Organization module), and `TicketGrantMode` (Registrations module)

**Non-Goals:**
- Email confirmation / notification on successful registration
- Registration cancellation or amendment
- Waitlist management
- Admin-initiated direct registration (organizers use the coupon/invite flow instead)

---

## Decisions

### D1 — Two separate commands for the two registration paths

Self-service and coupon-based registration diverge significantly in business rules (capacity
enforcement, window check, domain check, coupon validation, coupon redemption). Merging them
into one command with a mode discriminator would scatter `if mode == coupon` guards throughout
the handler and make tests harder to read.

**Decision:** `SelfRegisterAttendeeCommand` + `SelfRegisterAttendeeHandler` for self-service;
`RegisterWithCouponCommand` + `RegisterWithCouponHandler` for coupon-based. Each handler has
a focused responsibility and tests cover it cleanly.

*Alternative considered:* reuse the existing (commented-out) `RegisterAttendeeCommand` with a
`TicketGrantMode` parameter. Rejected — the mode flag leaks cross-cutting concerns into a
single handler and the `Privileged` grant mode has no meaning once admin registration is gone.

---

### D2 — EventRegistrationPolicy as a dedicated aggregate in the Registrations module

The registration window and allowed email domain are registration-specific settings. They are
managed by organizers via admin endpoints and are only ever read by the Registrations module
at registration time. Storing them inside `TicketedEvent` (Organization module) would cross
the module boundary unnecessarily.

**Decision:** Introduce `EventRegistrationPolicy` as a new EF-persisted aggregate in the
Registrations module, keyed by `TicketedEventId`. It stores `RegistrationWindow?` (open /
close `DateTimeOffset`) and `AllowedEmailDomain?` (nullable string). Policies are created or
updated via a single upsert-style admin endpoint. Self-service registration loads the policy
and enforces it; coupon registration skips it.

*Alternative considered:* add registration policy fields to `TicketedEvent` in the
Organization module. Rejected — the Registrations module owns registration rules; the
Organization module owns event identity and structure. Mixing them creates cross-module
coupling.

---

### D3 — Use slug as the ticket type identifier throughout the Registrations module

The coupon migration (`ReplaceTicketTypeIdsWithSlugs`) already moved `Coupon.AllowedTicketTypeSlugs`
from UUIDs to slugs. `IOrganizationFacade.GetTicketTypesAsync` returns slugs as the primary
identifier. The commented-out `EventCapacity` / `TicketCapacity` scaffolding still uses
`TicketTypeId` (UUID) — this inconsistency should be resolved.

**Decision:** `TicketCapacity` is keyed by `Slug` (string), not `TicketTypeId`. All ticket
type references in the Registrations module (registrations, capacity, coupon allowlists) use
the slug. The `TicketTypeSnapshot` value object (currently commented out with an ID-based key)
will also be replaced by a slug-keyed version.

*Rationale:* Slugs are stable across module boundaries and human-readable. Using a different
identifier in Registrations to the one the Organization module surfaces in its facade adds a
translation step with no benefit.

---

### D4 — Capacity sync via new Organization module events

The Registrations module needs to know the max capacity per ticket type to enforce limits, and
must track used capacity itself (the Organization module has no concept of "used" capacity). A
local `EventCapacity` aggregate maintains both values, with the max capacity kept in sync from
the Organization module via the outbox/messaging infrastructure already in place.

**Decision:**
- `AddTicketTypeHandler` in the Organization module publishes a `TicketTypeAddedModuleEvent`
  (via the message policy) when a ticket type is successfully added.
- `UpdateTicketTypeHandler` publishes a `TicketTypeCapacityChangedModuleEvent` when the
  capacity field changes.
- The Registrations module handles these events to upsert `TicketCapacity` entries into the
  corresponding `EventCapacity` aggregate.
- `EventCapacity` is initialized lazily: the first `TicketTypeAddedModuleEvent` for a given
  event creates the `EventCapacity` record if it doesn't already exist.

*Alternative considered:* query the Organization facade for max capacity at claim time and
only track used capacity locally. Rejected — it creates a runtime dependency on the
Organization module inside the critical registration path and doesn't remove the need for a
local tracking aggregate.

---

### D5 — Capacity enforcement via optimistic concurrency on EventCapacity

`EventCapacity` has a `Version` row-version token (the standard pattern in this codebase).
When `SelfRegisterAttendeeHandler` claims tickets, it loads `EventCapacity`, checks that each
`TicketCapacity` has remaining capacity, increments `UsedCapacity`, and commits. A concurrent
registration for the same event will fail with `DbUpdateConcurrencyException`, which the
infrastructure maps to a `ConcurrencyConflictError` → HTTP 409.

Coupon-based registrations **always increment** `UsedCapacity` (the registration counts
toward real-world occupancy) but **do not enforce** the limit — the claim succeeds even if
`UsedCapacity` would exceed `MaxCapacity`. This means the `EventCapacity` aggregate needs two
claim operations: an enforced one (self-service) and an uncapped one (coupon). The `Version`
token is advanced in both cases, so concurrent writes still contend correctly.

**Decision:** use optimistic concurrency for both paths. `TicketCapacity` exposes
`ClaimWithEnforcement()` (throws if sold out) and `ClaimUncapped()` (always increments).
`EventCapacity.Claim(enforce: bool)` delegates to the appropriate method per ticket.

*Alternative considered:* pessimistic locking (`SELECT FOR UPDATE`). Rejected — holds row
locks for the duration of the transaction, increasing contention; the established codebase
pattern is optimistic.

---

### D6 — "Already registered" enforced by a unique database constraint

A guard query before insert has a TOCTOU race under concurrent requests from the same email.
A unique constraint on `(event_id, email)` in the `registrations` table is atomic and
race-free.

**Decision:** add a composite unique constraint on `(event_id, email)`. The constraint
violation is mapped to `AlreadyExistsError` via `IPostgresExceptionMapping` in the
Registrations module.

*Note:* the scaffolded `RegistrationEntityConfiguration` has a unique index on `email` alone
— this is incorrect and will be replaced with the composite constraint.

---

### D7 — Extend TicketTypeDto with TimeSlots and Capacity

The registration handler needs time slot data for the overlap check and capacity data as a
cross-validation. Rather than adding a second facade call, the existing
`GetTicketTypesAsync` response is extended.

**Decision:** add `IReadOnlyList<string> TimeSlots` and `int? Capacity` to `TicketTypeDto` in
`Admitto.Module.Organization.Contracts`. The Organization facade implementation reads these
from the `TicketType` value object already stored on `TicketedEvent`.

### D8 — Coupon registration endpoint is anonymous

Coupons are emailed to specific attendees. Requiring authentication before redemption adds
friction (the attendee would need an account first) and contradicts the purpose of the invite
flow. Possession of the coupon code is treated as sufficient proof of invite.

**Decision:** the coupon registration endpoint requires no authentication. The coupon code
is the credential. Single-use enforcement (coupon marked redeemed on success) limits abuse.

---

### D9 — TicketCapacity.MaxCapacity is nullable; null means not available for self-service

Ticket types without an explicit capacity set are **not open for self-service registration**.
Organizers must assign a capacity before a ticket type becomes registerable via the public
endpoint. This is expressed by `TicketCapacity.MaxCapacity` being nullable:

- `MaxCapacity = null` → self-service registration rejected ("ticket type not available")
- `MaxCapacity = n` → capacity enforced; registration rejected when `UsedCapacity >= n`

Coupon-based registration bypasses this check entirely (null capacity = still registerable
via invite).

**Decision:** `TicketCapacity.MaxCapacity` is `int?`. The `ClaimWithEnforcement()` path
checks for null and throws "ticket type not available" before checking remaining capacity.
`ClaimUncapped()` (coupon path) always increments `UsedCapacity` regardless of `MaxCapacity`.

---

**[Concurrent registrations cause 409s on popular ticket types]** → Clients (the public
registration UI and any API consumers) must handle 409 gracefully and offer a retry. The
concurrency window is narrow (one transaction per registration), so collisions are rare in
practice for small free events.

**[Module event delivery delay leaves EventCapacity stale]** → If `TicketTypeAddedModuleEvent`
is delayed, the Registrations module may reject a registration as "unknown ticket type" even
though the type was recently added. The `OutboxDispatcher` attempts immediate in-process
delivery; the Worker host retries failed messages. The window of inconsistency is seconds, not
minutes.

**[Slug renames break capacity tracking]** → If an organizer renames a ticket type slug, the
Registrations module would treat it as a new ticket type and lose the used-capacity counter.
Slug renames are not currently supported in the Organization module (no `UpdateTicketTypeSlug`
operation), so this risk is deferred.

---

## Migration Plan

1. **Organization module migration** — drop `IsSelfService` and `IsSelfServiceAvailable`
   columns from the `TicketType` JSON structure. Existing JSON payloads that still contain
   these keys are harmlessly ignored by EF Core after the model change.

2. **Registrations module migration** — add:
   - `registrations` table (complete `RegistrationEntityConfiguration`, fix unique index to `(event_id, email)`)
   - `event_capacity` / `ticket_type_capacities` table/JSON (update to use slug key, nullable `max_capacity`)
   - `event_registration_policy` table for `EventRegistrationPolicy`

3. **Deploy order** — Admitto.Migrations runs first, then API + Worker. No rolling-update
   concerns since both registration endpoints are new (no existing clients to break). No data
   backfill required — deploying to a fresh instance.

---

## Open Questions

All questions resolved during design review. See D8 and D9.
