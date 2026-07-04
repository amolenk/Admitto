## Context

Ticket-confirmation resend is currently implemented as a Registrations-owned command with an Admin API endpoint. The handler validates a registration by `(TeamId, TicketedEventId, RegistrationId)`, requires `RegistrationStatus.Registered`, and enqueues `TicketConfirmationResendRequestedIntegrationEvent` with a caller-generated `ResendRequestId`. The Email module consumes that integration event and uses the existing ticket email rendering, EmailLog send-claim, and Worker-owned SMTP delivery pipeline.

Partner endpoints are mounted under `/api/events/{eventSlug}/...`, authenticated by `X-Api-Key`, and derive `TeamId` from the API-key principal. Existing Partner endpoints resolve `{eventSlug}` through `PartnerTicketedEventResolver` before dispatching commands that operate on internal event IDs.

## Goals / Non-Goals

**Goals:**

- Expose ticket-confirmation resend to trusted Partner API clients without changing email delivery semantics.
- Keep event and registration lookup scoped to the API-key owner's team.
- Reuse the existing Registrations command and Email module pipeline so idempotency, logging, and async delivery remain consistent with Admin API resends.
- Preserve endpoint-owned unit-of-work commits.

**Non-Goals:**

- Add anonymous public ticket resend.
- Add inline SMTP sending from the API host.
- Add a new Email module API or cross-module synchronous email send call.
- Change the resend email content, EmailLog schema, or idempotency key format.
- Add attendee identity verification beyond possession of a valid team API key and registration id.

## Decisions

### Add a Partner API endpoint adapter over the existing command

Implement a Partner endpoint in the existing `RequestTicketConfirmationResend` use-case folder, under a `PartnerApi/` surface namespace. The endpoint SHALL map `POST /api/events/{eventSlug}/registrations/{registrationId}/ticket-email/resend`, read `TeamId` from `HttpContext.User.GetRequiredTeamId()`, resolve the public event slug with `PartnerTicketedEventResolver`, dispatch `RequestTicketConfirmationResendCommand`, commit the keyed Registrations unit of work, and return `202 Accepted`.

Alternative considered: create a separate partner-specific command. This would duplicate registration validation and outbox enqueue behavior without adding different business rules, increasing the risk that Admin and Partner resends diverge.

### Keep Registrations as the resend request owner

The Partner endpoint SHALL remain in the Registrations module because the authoritative registration state and event slug resolution are Registrations-owned. Email remains a downstream consumer of the existing integration event.

Alternative considered: expose an Email-owned Partner endpoint that validates the registration through a facade. That would invert ownership and require Email to make eligibility decisions that belong to Registrations.

### Use the existing Partner API security and scope model

The endpoint SHALL rely on the existing `/api` group API-key authentication, rate limiting, validation filter, and `PartnerTicketedEventResolver`. A valid key for another team receives normal not-found behavior because the event slug is resolved only within the key owner's `TeamId`.

Alternative considered: include team id or team slug in the route. This conflicts with the documented Partner API route model and would create a second scoping mechanism.

### Do not require a request body

The endpoint needs only the route registration id and the authenticated team/event scope. The `ResendRequestId` remains server-generated per accepted request, matching the Admin endpoint behavior.

Alternative considered: accept a client-provided idempotency key. That is unnecessary for current resend semantics and would expand the API contract without a concrete need.

## Risks / Trade-offs

- [Risk] Partner websites can trigger repeated ticket-email resends for a known registration id. → Mitigation: the endpoint remains protected by team API keys and existing Partner rate limiting; each accepted request is intentionally a new resend, matching Admin behavior.
- [Risk] Registration ids are bearer-like secrets in partner self-service flows. → Mitigation: the endpoint never bypasses team/event scoping and only acts on registrations under the API-key owner's team and resolved event.
- [Risk] Email delivery failure might be misread as endpoint failure. → Mitigation: preserve `202 Accepted` semantics and async Worker-owned delivery; operational status remains visible through existing EmailLog behavior.

## Migration Plan

No data migration is required. Deploying the API adds the new Partner route; rollback removes only that route and leaves existing Admin resend behavior unchanged.

## Open Questions

- None.
