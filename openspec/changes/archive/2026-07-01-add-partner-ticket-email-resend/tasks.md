## 1. Partner Endpoint

- [x] 1.1 Add a `PartnerApi` endpoint for `POST /api/events/{eventSlug}/registrations/{registrationId}/ticket-email/resend` in the existing `RequestTicketConfirmationResend` use-case folder.
- [x] 1.2 In the endpoint, read `TeamId` from the API-key principal, resolve `{eventSlug}` with `PartnerTicketedEventResolver`, dispatch `RequestTicketConfirmationResendCommand`, commit the Registrations unit of work, and return `202 Accepted`.
- [x] 1.3 Wire the new endpoint into `MapRegistrationsPartnerEndpoints()` under `/api/events/{eventSlug}`.

## 2. Tests

- [x] 2.1 Add API coverage that a valid team API key for the event's team receives `202 Accepted` and the resend request is durably enqueued.
- [x] 2.2 Add API coverage for missing, invalid, or revoked API keys returning `401` with no resend work.
- [x] 2.3 Add API coverage that an API key from another team gets not-found behavior and no resend work.
- [x] 2.4 Add API or handler coverage for missing registration and non-`Registered` registration rejection from the Partner route.

## 3. Verification

- [x] 3.1 Run `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [x] 3.2 Run the targeted API/Registrations tests that cover Partner endpoint behavior.
- [x] 3.3 Confirm OpenAPI generation includes the new Partner endpoint if API contract artifacts are regenerated during implementation.
