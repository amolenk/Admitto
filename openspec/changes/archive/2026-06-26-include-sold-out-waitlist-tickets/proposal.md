## Why

The public ticket-type response currently exposes internal capacity counters (`maxCapacity`, `usedCapacity`) and two internal waitlist flags (`waitlistEnabled`, `waitlistMode`). Partner event websites have to reconstruct the attendee action from those four fields, which is error-prone and easy to get wrong. Collapsing them into a `soldOut` flag and a single `requiresWaitlist` flag gives partner sites the only two signals they actually need to render a ticket and pick the correct submission path.

## What Changes

- **BREAKING**: Replace `maxCapacity` / `usedCapacity` in the public ticket response with a single `soldOut` flag, and replace `waitlistEnabled` / `waitlistMode` with a single `requiresWaitlist` flag.
- Keep returning all self-service-enabled ticket types regardless of capacity (current behavior), so partner sites can render a stable catalog including sold-out tickets.
- Derive `soldOut` and `requiresWaitlist` from existing domain state in the public ticket-type query; no new persisted fields.
- Keep registration semantics unchanged: tickets with `soldOut = false` are registerable; tickets with `requiresWaitlist = true` are waitlistable; the server remains the source of truth at submission time.
- Add regression coverage for the public ticket-type response so the `soldOut` / `requiresWaitlist` derivation is locked in for all three states.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `attendee-registration`: The public ticket discovery response replaces raw capacity and internal waitlist fields with `soldOut` and `requiresWaitlist`.

## Impact

- Affects the Registrations module public ticket-type query and the public API response contract under `/api/events/{eventId}/ticket-types`.
- **BREAKING** for the public response shape: regenerate the OpenAPI spec and the Admin UI SDK, and update any consumer of the old fields.
- Affects API tests for the public ticket-type response.
- Does not require database schema changes, new dependencies, or changes to transaction boundaries.
