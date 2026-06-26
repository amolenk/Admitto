## Context

Public attendees discover event ticket choices through `GET /api/events/{eventId}/ticket-types`, authenticated by `X-Api-Key` and scoped to the key owner's `TeamId`. The Registrations module owns the ticket catalog and the public discovery query. A previous in-progress change reshaped the response toward `soldOut` and `requiresWaitlist`; this change refines that contract to one action-oriented `status` value instead.

The status can be derived from existing domain state. `TicketType.IsSoldOut` represents bounded capacity exhaustion, and `WaitlistMode` represents the runtime state where a sold-out ticket accepts waitlist joins. The public query materializes ticket types before mapping, so using a computed domain property in the mapping does not introduce EF translation risk.

## Goals / Non-Goals

**Goals:**

- Return every self-service-enabled ticket type from the public ticket-type listing, including sold-out tickets.
- Replace public `soldOut` and `requiresWaitlist` booleans with a single `status` string.
- Use exactly three public status values: `available`, `waitlist`, and `soldOut`.
- Keep submission-time validation authoritative; the status is a client rendering and action hint, not a reservation.
- Preserve existing registration and waitlist submission behavior.

**Non-Goals:**

- Do not change ticket capacity claiming or waitlist activation rules.
- Do not add a new database field or persist status.
- Do not change admin ticket-type listing or management responses.
- Do not introduce a new availability enum for internal registration conflict responses.

## Decisions

- Expose `status` as a string in the public DTO.
  - Rationale: OpenAPI and generated TypeScript clients naturally represent string enums/unions for public contracts, and partner sites can switch on one field.
  - Alternative considered: keep two booleans. That still leaves clients combining fields and allows invalid combinations to be represented.

- Derive `status` in `GetPublicTicketTypesHandler` from existing ticket state.
  - Mapping order: `WaitlistMode` -> `"waitlist"`; otherwise `IsSoldOut` -> `"soldOut"`; otherwise `"available"`.
  - Rationale: `WaitlistMode` already implies the ticket is sold out and accepting waitlist joins. Checking it first makes the public action explicit.
  - Alternative considered: derive from raw capacity and `WaitlistEnabled`. That duplicates domain rules and can incorrectly advertise a waitlist when waitlist mode is not active.

- Keep `TicketType.IsSoldOut` as the single domain expression for sold-out state.
  - Rationale: The sold-out rule is domain-owned and reused by public mapping and capacity-related logic.
  - Alternative considered: inline the comparison in the public query. This would be a smaller local change but repeats a core rule.

- Do not add a capacity or actionability filter to the public query.
  - Rationale: Partner sites need a stable catalog and may render sold-out tickets as unavailable rather than hiding them.
  - Alternative considered: hide sold-out non-waitlist tickets. That changes visibility and conflicts with the current product direction.

## Risks / Trade-offs

- Existing public clients consuming `soldOut` / `requiresWaitlist` will break -> Mitigation: mark the change breaking, regenerate OpenAPI and SDKs, and update known consumers.
- A string status can be misspelled by handwritten clients -> Mitigation: define exact allowed values in the OpenAPI schema and generated SDK type.
- Status can become stale between discovery and submission -> Mitigation: keep submission-time registration and waitlist validation authoritative and retain existing conflict behavior.
- `available` hides exact remaining capacity -> Mitigation: exact capacity is intentionally out of scope; add a deliberate remaining-count field later only if needed.
