## Context

Public attendees discover event ticket choices through `/api/events/{eventId}/ticket-types`, authenticated by `X-Api-Key` and scoped to the key owner's `TeamId`. The query today returns every `SelfServiceEnabled` ticket type (no capacity filter) and exposes raw `maxCapacity`, `usedCapacity`, `waitlistEnabled`, and `waitlistMode`.

Partner event sites have to combine those four fields to decide whether a ticket can be registered, must be waitlisted, or is just sold out. That is the integration pain this change targets: the response shape, not visibility. Internally, `WaitlistMode` is already only `true` when a `WaitlistEnabled` ticket type has reached a bounded capacity (`TicketCatalog.cs`), and a ticket type with no `MaxCapacity` can never be sold out, so both public flags can be derived directly from existing state.

## Goals / Non-Goals

**Goals:**

- Keep returning every `SelfServiceEnabled` ticket type, regardless of capacity (no behavior change to which rows are returned).
- Replace public exposure of capacity numbers and separate internal waitlist flags with simple `soldOut` and `requiresWaitlist` flags.
- Preserve existing registration and waitlist submission invariants; this change only reshapes the public response.
- Add tests that lock in the three response states: available (`soldOut = false`), sold-out waitlistable (`soldOut = true`, `requiresWaitlist = true`), and sold-out non-waitlist (`soldOut = true`, `requiresWaitlist = false`).

**Non-Goals:**

- Do not change capacity-claim behavior or allow direct registration for WaitlistOnly ticket types.
- Do not add new waitlist endpoints or database columns.
- Do not change admin ticket-type listing behavior.

## Decisions

- Keep the behavior in the existing Registrations public ticket-type query.
  - Rationale: ticket type data and waitlist state are owned by the Registrations module, and the existing `GetPublicTicketTypes` slice is already the public discovery surface.
  - Alternative considered: add a separate waitlist-discovery endpoint. This would duplicate ticket-type visibility logic and make clients merge two public lists.

- Keep returning every `SelfServiceEnabled` ticket type, regardless of remaining capacity (unchanged).
  - Rationale: partner websites own presentation and may want to show sold-out tickets as unavailable rather than have them disappear from the catalog. This is already the current behavior; the change does not add a capacity filter.
  - Alternative considered: return only currently actionable tickets. That would be a new restriction that hides sold-out tickets and prevents a stable catalog.

- Derive `soldOut` from `MaxCapacity is not null && UsedCapacity >= MaxCapacity` and expose it instead of `maxCapacity` / `usedCapacity`.
  - Rationale: partner websites only need a presentation/action signal, not raw capacity internals. A null `MaxCapacity` is unbounded and therefore never sold out, matching the existing claim logic in `TicketType.ClaimWithEnforcement`.
  - Alternative considered: keep exposing `maxCapacity` and `usedCapacity`. That asks every integration to duplicate sold-out calculation and exposes more domain detail than necessary.

- Centralize the sold-out rule as a `TicketType.IsSoldOut` computed property rather than inlining the comparison in the query.
  - Rationale: the expression `MaxCapacity is not null && UsedCapacity >= MaxCapacity.Value` is already duplicated across `TicketType` and `TicketCatalog`; a single computed property keeps the public mapping and the domain in agreement and avoids a sixth copy. The public query materializes the catalog and maps in memory, so non-translatability of a computed property is not a concern here.
  - Alternative considered: inline the comparison in `GetPublicTicketTypesHandler`. Simpler diff, but adds yet another copy of a rule that should have one owner.

- Expose `requiresWaitlist` as a direct projection of `WaitlistMode`.
  - Rationale: `WaitlistMode` is already the runtime "sold out and currently accepting waitlist joins" state, and is only ever `true` for a `WaitlistEnabled`, bounded, at-capacity ticket type. So `requiresWaitlist` implies `soldOut`, and partner sites get one unambiguous action signal.
  - Alternative considered: infer `requiresWaitlist` from sold-out capacity alone. That would incorrectly prompt a waitlist join for sold-out ticket types that do not accept waitlist joins.
  - Alternative considered: keep exposing `waitlistEnabled` and `waitlistMode`. That leaks domain mechanics and makes external clients combine flags correctly.
  - Alternative considered: expose a broader availability enum. That may become useful later, but two booleans match the current client decision and keep this change small.

## Risks / Trade-offs

- Existing public clients consume `maxCapacity` / `usedCapacity` / `waitlistEnabled` / `waitlistMode` today -> Mitigation: treat the response change as breaking, regenerate the OpenAPI spec and Admin UI SDK, and update consumers to `soldOut` / `requiresWaitlist`.
- Removing raw capacity numbers hides exact availability (e.g. "3 left") from partner sites -> Mitigation: out of scope here; if a remaining-count display is needed later, add a dedicated field deliberately rather than re-exposing internal counters.
- Capacity and waitlist state can change between listing and submission -> Mitigation: keep submission-time validation authoritative and return existing stale ticket-state conflicts when clients submit outdated choices.
- A returned ticket with `requiresWaitlist = false` may still be sold out and not directly registerable -> Mitigation: expose `soldOut` directly so clients can render sold-out state and avoid direct registration for unavailable tickets.
