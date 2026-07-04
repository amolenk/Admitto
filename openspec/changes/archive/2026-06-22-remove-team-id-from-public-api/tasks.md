## 1. Shared API-Key Team Scope

- [x] 1.1 Add a shared API-key team-claim constant/helper in `Admitto.Core.Shared.Application.Auth` or equivalent Core-owned shared location.
- [x] 1.2 Update `ApiKeyAuthenticationHandler` to emit the shared team claim instead of its private claim constant.
- [x] 1.3 Add or update fail-closed team-claim extraction behavior for public endpoint use.

## 2. Public Route Mapping

- [x] 2.1 Change `MapRegistrationsPublicEndpoints` to group public endpoints under `/events/{eventId:guid}` instead of `/teams/{teamId:guid}/events/{eventId:guid}`.
- [x] 2.2 Remove `ApiKeyTeamScopeFilter` from public endpoint configuration and delete the unused filter.
- [x] 2.3 Remove `.AllowAnonymous()` from public endpoint mappings so every `/api/...` endpoint uses the public API-key authorization policy.

## 3. Public Endpoint Handlers

- [x] 3.1 Update OTP request/verify endpoints to derive `TeamId` from the API-key principal and bind only `eventId` from the route.
- [x] 3.2 Update self-service and coupon registration endpoints to derive `TeamId` from the API-key principal and update created `Location` URLs to the new route shape.
- [x] 3.3 Update self-cancel and self-change ticket endpoints to derive `TeamId` from the API-key principal.
- [x] 3.4 Update public ticket type, waitlist, public coupon details, and QR-code endpoints to derive `TeamId` from the API-key principal.

## 4. Tests

- [x] 4.1 Update public API test fixtures and hard-coded routes to `/api/events/{eventId}/...`.
- [x] 4.2 Update API-key authorization tests to expect missing/invalid/revoked keys to return 401 on the new route shape.
- [x] 4.3 Replace the old route-team mismatch test with coverage showing a valid key for another team cannot access the event, using the normal not-found behavior.
- [x] 4.4 Add or update tests proving QR-code and public coupon details endpoints require `X-Api-Key`.
- [x] 4.5 Add or update tests proving old `/api/teams/{teamId}/events/{eventId}/...` public routes return 404.

## 5. Generated Clients And Documentation

- [x] 5.1 Regenerate the Admin UI OpenAPI SDK through the approved Aspire-backed workflow after backend route changes.
- [x] 5.2 Update arc42 cross-cutting/runtime documentation for public API key team-scope resolution and route shape.
- [x] 5.3 Ensure OpenSpec specs remain aligned with the final implemented public route and auth behavior.

## 6. Verification

- [x] 6.1 Run `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj` first.
- [x] 6.2 Run targeted API tests covering public routes and API-key authentication.
- [x] 6.3 Run any targeted Registrations tests affected by endpoint contract changes.
