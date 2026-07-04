## Context

Partner API endpoints already live under `/api/events/{eventSlug}/...`, require `X-Api-Key`, and resolve the event slug within the authenticated API key owner's team scope before invoking Registrations handlers. The admin registration detail use case already has the needed read shape available internally, but its HTTP payload includes admin-only metadata such as reconfirmation timestamps, cancellation reason, and activity log entries.

Existing partner self-service modification routes identify the registration by `registrationId` in the URL and treat that value as the attendee-held bearer credential for the registration itself. The new endpoint is a read-only Registrations module capability for trusted partner websites that need enough registration data to prefill an edit form before submitting changes.

## Goals / Non-Goals

**Goals:**
- Expose a Partner API `GET` endpoint for one registration scoped by API-key team, event slug, and registration ID.
- Preserve the existing Partner API convention where `registrationId` is the attendee bearer credential and `X-Api-Key` authorizes the partner website/team integration.
- Return only the reduced DTO needed to prefill a registration edit form: registration identity, attendee name/email, status, current ticket selection, and additional details.
- Reuse existing Partner API event resolution and read-model/query conventions where possible.
- Preserve the admin detail endpoint and full admin DTO behavior.

**Non-Goals:**
- No changes to registration creation, cancellation, ticket-change, reconfirmation, or QR-code flows.
- No new authentication mechanism, email-verification-token requirement, or public anonymous access.
- No database schema changes.
- No exposure of admin-only fields such as activity logs, cancellation reason, registered timestamp, or reconfirmation metadata.

## Decisions

1. Add a dedicated Partner API endpoint at `GET /api/events/{eventSlug}/registrations/{registrationId}`.

   This mirrors the existing self-service registration modification route and avoids introducing a second lookup credential. Alternative considered: expose lookup by attendee email plus an email-verification token. That was rejected because partner edit links already carry `registrationId`, and requiring a fresh email token would make prefill harder without matching the existing bearer-link model.

2. Use the existing Partner event resolver before querying registration details.

   Endpoint code should extract `TeamId` from the API-key principal, resolve `{eventSlug}` within that team, then query by `(teamId, ticketedEventId, registrationId)` or equivalent scoped criteria. Alternative considered: let the handler resolve the slug. That was rejected because existing Partner endpoints centralize route slug resolution in endpoint code and handlers operate on IDs.

3. Treat `registrationId` as the registration bearer credential and require no `Authorization` bearer token.

   This follows the documented registration-bound public link model for partner mutation endpoints. The API key authorizes the partner integration and scopes event resolution; the high-entropy `registrationId` authorizes access to that attendee registration. Requests with missing or invalid API keys still return 401 through Partner API authentication. Unknown registrations, registrations from another event, and valid API keys from another team all return not found.

4. Return a Partner-specific reduced DTO rather than the full admin detail DTO.

   The shape intentionally excludes admin-only fields and should include machine-usable current ticket identifiers for form prefill. This should live in a dedicated Partner lookup slice because its scoping and payload semantics differ from the admin registration detail slice.

5. Treat missing event or registration as not found.

   A valid API key for another team resolves within its own team scope and receives normal not-found behavior. This preserves the fail-closed behavior documented for Partner API routes.

## Risks / Trade-offs

- Personal data exposure through Partner API -> Keep the route behind `X-Api-Key`, scope all lookups by the API-key team and event, treat `registrationId` as a high-entropy bearer credential, and return only the reduced DTO.
- Registration identifier leakage -> This endpoint intentionally follows the existing self-service edit model where attendee links carry `registrationId`; do not add email or broad listing lookup behavior.
- DTO naming overlap with the admin registration detail DTO -> Prefer a Partner-specific DTO or clearly scoped type location if the existing admin DTO carries extra fields.
- Route collision with existing registration mutation routes -> Use `GET` on the same registration resource path as the existing `PUT` update route and avoid adding overlapping `GET` templates.
