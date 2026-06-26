## Why

The public ticket-type response currently requires partner event websites to interpret separate availability signals. A single `status` field gives clients one stable action-oriented value to render and route registration vs waitlist behavior.

## What Changes

- **BREAKING**: Replace the public ticket response's `soldOut` / `requiresWaitlist` fields with a single `status` string.
- Return `status = "available"` when the ticket type can be directly selected for registration.
- Return `status = "waitlist"` when the ticket type is sold out and currently accepts waitlist joins.
- Return `status = "soldOut"` when the ticket type is sold out and does not currently accept waitlist joins.
- Keep returning all self-service-enabled ticket types regardless of capacity, so sold-out tickets remain visible to partner websites.
- Keep registration and waitlist submission semantics authoritative on the server at submission time.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `attendee-registration`: Public ticket discovery exposes a single availability `status` field instead of separate sold-out and waitlist booleans.

## Impact

- Affects the Registrations module public ticket-type query and public API response contract under `/api/events/{eventId}/ticket-types`.
- Requires OpenAPI spec and Admin UI SDK regeneration.
- Requires updating any generated or handwritten consumers of `soldOut` / `requiresWaitlist` to use `status`.
- Requires API tests for the three public ticket status values and omission of the old fields.
- Does not require database schema changes, new dependencies, or changes to transaction boundaries.
