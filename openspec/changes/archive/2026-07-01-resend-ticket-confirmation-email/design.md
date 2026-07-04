## Context

Ticket-confirmation emails are currently sent by the Email module when it handles `AttendeeRegisteredIntegrationEvent`. The handler builds the same `TicketConfirmation` template parameters from the registration event payload plus Email-owned event rendering context, then delegates to `SendEmailCommand`, which creates an `EmailLog` claim and enqueues `DeliverEmailCommand` for Worker-owned SMTP delivery.

Admin registration detail and attendee email history already use routes scoped by `{teamId}`, `{eventId}`, and `{registrationId}`. The resend action should fit into that surface and preserve the existing architecture constraints: API endpoints own transaction commits, command handlers do not commit unit-of-work objects, Email owns email rendering/delivery, and modules do not read sibling DbContexts directly.

## Goals / Non-Goals

**Goals:**

- Let an authenticated team member request a resend of the ticket-confirmation email for an existing registered attendee through the Admin API.
- Reuse the existing built-in `TicketConfirmation` template, Email rendering context, `EmailLog`, outbox, and Worker delivery pipeline.
- Allow resends even when the original registration-triggered ticket email has a terminal `Sent` log row.
- Keep duplicate delivery retries for a single resend request idempotent.
- Make the resend visible in attendee email history as another `ticket` email log entry.

**Non-Goals:**

- No public duplicate-registration response or public resend endpoint in this change.
- No Admin UI button or generated UI SDK update unless implementation later chooses to expose it in the UI.
- No direct SMTP send from the API host.
- No new email template type for resends.
- No attempt to revoke or replace earlier ticket links; generated links remain derived from current Email rendering context.

## Decisions

1. Expose the action as a registration-scoped Admin API endpoint.

   Use `POST /admin/teams/{teamId}/events/{eventId}/registrations/{registrationId}/ticket-email/resend` so authorization and route-scope checks match existing registration detail and attendee email history. The endpoint should require at least organizer-level team membership, resolve the registration snapshot, enqueue a Registrations integration event, commit the Registrations unit of work, and return `202 Accepted` once the resend request is durable.

   Alternative considered: expose this as an Email module route under `/emails`. That makes the action less discoverable from registration detail and still requires registration existence validation, so registration-scoped routing is clearer.

2. Model the resend as Email-owned durable work fed by a Registrations-owned snapshot.

   The resend command needs occurrence-specific facts that Email cannot infer from its event projection: recipient address, attendee name, registration id, and current ticket names. The Registrations resend handler should load these facts with a scoped query and publish them in a `TicketConfirmationResendRequested` integration event. Email still owns rendering and delivery, and still uses Email-owned context for team/event branding and links.

   Alternative considered: Email directly queries Registrations tables. This violates module boundaries and is rejected.

3. Use a resend-specific idempotency key.

   The original registration email uses `attendee-registered:{registrationId}:{registeredAt}`. A manual/admin resend must not be blocked by that original key, so it should use a distinct key such as `ticket-confirmation-resend:{registrationId}:{resendRequestId}`. The endpoint can generate `resendRequestId` per accepted request, allowing each admin click to create one resend, while duplicate processing of the same request remains idempotent through `EmailLog` uniqueness.

   Alternative considered: reuse the original registration idempotency key. That would make the resend a no-op after the first successful ticket email and would not satisfy the resend use case.

4. Do not send SMTP inline from the API host.

   The endpoint should only persist the Registrations outbox request and return. The Worker handles the integration event, creates the EmailLog/outbox work through `SendEmailCommand`, and performs SMTP via the existing `DeliverEmailCommand` handler and host capability gating.

   Alternative considered: resolve `IEmailSender` in the API host for immediate feedback. This conflicts with the current Email host capability model and couples API latency to SMTP delivery.

## Risks / Trade-offs

- [Risk] Repeated admin clicks can intentionally send multiple ticket emails. → Mitigation: each accepted resend request uses a distinct idempotency key and is auditable through `EmailLog`; future UI can add confirmation or throttling if needed.
- [Risk] Registration data and Email rendering context can diverge briefly. → Mitigation: use Registrations for attendee/ticket facts and Email projections for reusable event/team facts, matching the existing transactional email model.
- [Risk] SMTP failure after accepted response may surprise callers. → Mitigation: keep response semantics asynchronous and rely on existing email history/logging to show pending/failed/sent status.
- [Risk] Future public duplicate-registration resend flow may need stricter abuse controls. → Mitigation: this change only creates the admin capability; public exposure should add rate limiting and email ownership checks separately.
