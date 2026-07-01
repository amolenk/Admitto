## 1. Registration Snapshot

- [x] 1.1 Add a Registrations resend handler query for ticket-confirmation resend facts scoped by `teamId`, `eventId`, and `registrationId`.
- [x] 1.2 Ensure the handler publishes only recipient email, first name, last name, ticket names, and registration id without exposing unrelated registration data.
- [x] 1.3 Add integration tests for found, missing, wrong-scope, and non-registered registration cases.

## 2. Email Resend Use Case

- [x] 2.1 Add an Email use case that accepts the registration resend facts and a generated resend request id.
- [x] 2.2 Build `TicketConfirmation` parameters using registration facts plus `GetEventEmailRenderingContextQuery`.
- [x] 2.3 Use a resend-specific idempotency key distinct from `attendee-registered:{registrationId}:{registeredAt}`.
- [x] 2.4 Delegate to `SendEmailCommand` so `EmailLog` claims and `DeliverEmailCommand` outbox work use the existing pipeline.
- [x] 2.5 Add Email integration tests proving original sent logs do not suppress resends and duplicate processing of the same resend request is idempotent.

## 3. Admin API Endpoint

- [x] 3.1 Add `POST /admin/teams/{teamId}/events/{eventId}/registrations/{registrationId}/ticket-email/resend` under the registration admin route group.
- [x] 3.2 Require team-membership authorization consistent with registration detail and attendee email history endpoints.
- [x] 3.3 Generate a resend request id in the endpoint or command boundary, dispatch the use case, commit the Registrations module unit of work, and return `202 Accepted`.
- [x] 3.4 Map missing registrations to `404 Not Found` and non-registered registrations to the established problem/error response pattern.
- [x] 3.5 Wire the endpoint in the module endpoint registration entry point.

## 4. Verification

- [x] 4.1 Add API tests for unauthenticated, forbidden, not-found, non-registered, and accepted resend scenarios.
- [x] 4.2 Verify accepted resends appear through existing attendee email history as normal `ticket` email log entries.
- [x] 4.3 Run `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj` first and fix any violations.
- [x] 4.4 Run targeted Core integration and API tests for registrations/email resend behavior.
