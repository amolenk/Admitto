## Why

Ticket-confirmation resend is currently available only through the Admin API, which prevents trusted partner event websites from helping attendees recover a missed or lost ticket email. Partner integrations already authenticate with team-scoped API keys and operate on public event slugs, so the same durable resend flow should be available through that surface.

## What Changes

- Add a Partner API endpoint for requesting a ticket-confirmation resend for an existing registration in an event resolved from `/api/events/{eventSlug}`.
- Require a valid active `X-Api-Key`; derive `TeamId` from the API-key principal and resolve the event slug within that team scope.
- Reuse the existing Registrations-owned resend command/outbox integration event and Email module send pipeline.
- Return `202 Accepted` once resend work is durably requested; SMTP delivery remains asynchronous and Worker-owned.
- Preserve existing rejection behavior for missing registrations, wrong event/team scope, and non-`Registered` registrations.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `email-sending`: Add Partner API access to the existing ticket-confirmation resend capability.

## Impact

- `Admitto.Api`: Partner route wiring and API-key-authenticated endpoint behavior.
- `Admitto.Core/Registrations`: Partner API endpoint slice or endpoint adapter that resolves team/event scope and invokes the existing resend use case.
- `Admitto.Core/Email`: No new SMTP behavior; existing resend integration-event handler and EmailLog idempotency remain authoritative.
- Tests: API tests for Partner authentication/scope behavior and targeted resend behavior coverage where needed.
