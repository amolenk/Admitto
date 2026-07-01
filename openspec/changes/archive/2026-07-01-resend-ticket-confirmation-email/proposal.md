## Why

Attendees sometimes lose or cannot find their original ticket-confirmation email after registering. We need an admin-triggerable resend path now so a later duplicate self-registration flow can tell an attendee they are already registered and offer to send the ticket email again instead of creating another registration.

## What Changes

- Add an Admin API endpoint that resends the built-in `TicketConfirmation`/`ticket` email for an existing registration scoped to a team and ticketed event.
- Reuse the existing Email module ticket-email composition and Worker-owned SMTP delivery pipeline; the API request should enqueue durable email work, not send SMTP inline.
- Use a new resend-specific idempotency key so a resend is allowed even when the original registration-triggered ticket email was already sent, while duplicate processing of the same resend request remains idempotent.
- Return an accepted/success response once the resend has been durably requested; delivery remains asynchronous and visible through the existing attendee email history.
- Do not add public duplicate-registration behavior yet; this change only creates the admin capability that future public behavior can call or mirror.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `email-sending`: add an admin-triggered ticket-confirmation resend flow for an existing registration.

## Impact

- `Admitto.Api`: new admin route under the registration detail surface, protected by team-membership authorization and existing route-scope resolution.
- `Admitto.Core/Registrations`: registration lookup/snapshot query or facade data needed to verify the registration and supply occurrence-specific ticket email facts.
- `Admitto.Core/Email`: new command/use case to request a ticket resend, create an `EmailLog` claim, and enqueue the existing delivery command with resend idempotency.
- `Admitto.Worker`: no new host behavior expected; existing Email-capable worker delivery should process the queued work.
- Tests: architecture tests, Email integration tests for durable resend/idempotency, and API tests for authorization/not-found/success behavior.
