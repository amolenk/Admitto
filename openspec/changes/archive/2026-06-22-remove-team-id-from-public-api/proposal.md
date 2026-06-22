## Why

Public API clients already authenticate with a team-scoped API key, and the API key authentication handler resolves the owning team into the authenticated principal. Keeping the team ID in public URLs duplicates that scope, forces an extra route-vs-claim guard, and exposes an unnecessary identifier in every public integration URL.

## What Changes

- **BREAKING**: Public API routes will remove the team segment and use `/api/events/{eventId}/...` instead of `/api/teams/{teamId}/events/{eventId}/...`.
- Public endpoint handlers will derive `TeamId` from the authenticated API-key principal instead of binding it from the route.
- All public endpoints under `/api/...` will require a valid active `X-Api-Key`, including QR-code and public coupon details endpoints.
- The route-vs-claim `ApiKeyTeamScopeFilter` will be removed because there is no longer a route team ID to compare.
- Specs that describe anonymous public endpoints or pre-`/api` route shapes will be corrected to the API-key-protected `/api/...` contract.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `team-api-keys`: Public API key scope is derived solely from the key owner claim; public routes no longer include team ID or team slug.
- `email-otp-verification`: OTP public endpoints move under `/api/events/{eventId}/...` and require API-key authentication.
- `attendee-registration`: Public self-service and coupon registration endpoints move under `/api/events/{eventId}/...` and derive team scope from the API key.
- `self-service-cancel-registration`: Self-cancel endpoint moves under `/api/events/{eventId}/...` and requires API-key authentication.
- `self-service-change-tickets`: Self-service ticket-change endpoint moves under `/api/events/{eventId}/...` and requires API-key authentication.
- `waitlist`: Public waitlist endpoints move under `/api/events/{eventId}/...` and require API-key authentication.
- `attendee-qr-code`: QR-code endpoint is no longer anonymous; it moves under `/api/events/{eventId}/...` and requires API-key authentication in addition to signature validation.
- `coupon-management`: Public coupon details endpoint is no longer anonymous; it moves under `/api/events/{eventId}/...` and requires API-key authentication.
- `ticket-type-management`: Public ticket type listing moves under `/api/events/{eventId}/...` and requires API-key authentication.

## Impact

- Public API clients must update URLs to remove `/teams/{teamId}` and continue sending `X-Api-Key`.
- Backend public endpoint route mapping and endpoint signatures change in the Registrations module.
- API authentication plumbing changes by deleting `ApiKeyTeamScopeFilter` and centralizing API-key team claim access for endpoint code.
- OpenAPI output changes; generated clients, including the Admin UI SDK, must be regenerated through the approved Aspire-backed workflow.
- API tests and public route fixtures must be updated to the new route shape and protected-endpoint expectations.
