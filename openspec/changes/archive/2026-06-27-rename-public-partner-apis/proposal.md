## Why

Admitto now has three distinct HTTP audiences, but the current naming uses "public" for both API-key-protected partner integrations and anonymous attendee-facing links. Clarifying the boundary reduces security ambiguity and makes the new direct event-link surface easier to reason about.

## What Changes

- Rename the API-key-protected `/api/...` surface from Public API to Partner API in code, OpenAPI tags, tests, and documentation while preserving existing routes.
- Treat anonymous `/e/...` routes as the Public API.
- Rename the current `ResolvePublicEventLink` slice to `DirectPublicEventLinks`.
- Extend anonymous public event links to resolve registration, cancellation, edit, and QR-code routes:
  - `GET /e/{eventSlug}` redirects to the configured event website URL.
  - `GET /e/{eventSlug}/register` redirects to `{websiteBaseUrl}/register`.
  - `GET /e/{eventSlug}/cancel/{registrationId}` redirects to `{websiteBaseUrl}/cancel/{registrationId}`.
  - `GET /e/{eventSlug}/edit/{registrationId}` redirects to `{websiteBaseUrl}/edit/{registrationId}`.
  - `GET /e/{eventSlug}/qr-code/{registrationId}` returns the registration QR-code PNG.
- Move QR-code retrieval from the API-key-protected Partner API to the anonymous Public API and introduce proper query/handler classes for it.
- Update architecture documentation to describe Admin, Partner, and Public API trust boundaries, including the `tickets.admitto.org` custom-domain/host-alias deployment expectation.
- **BREAKING**: Remove or stop exposing the current API-key-protected QR-code endpoint at `GET /api/events/{eventId}/registrations/{registrationId}/qr-code`.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `public-event-links`: Anonymous Admitto-owned event links gain direct register, cancel, edit, and QR-code routes under `/e/{eventSlug}`.
- `attendee-qr-code`: QR-code retrieval moves from API-key-protected Partner API routing to anonymous Public API routing and resolves events by public slug.
- `team-api-keys`: API-key-protected `/api/...` endpoints are renamed from Public API to Partner API without changing route shape.

## Impact

- `src/Admitto.Api/Endpoints/PublicEndpoints.cs` and related endpoint grouping/naming.
- `src/Admitto.Core/Registrations/RegistrationsModule.cs` endpoint mapping and OpenAPI tags.
- Registrations use-case folders for `ResolvePublicEventLink` and `GetQRCode`.
- API tests for public event links, QR-code retrieval, API-key authentication, and route exposure.
- Generated Admin UI SDK/OpenAPI output if route names or tags are exposed there.
- `docs/arc42/06-runtime-view.md`, `docs/arc42/07-deployment-view.md`, and `docs/arc42/08-crosscutting-concepts.md`.
