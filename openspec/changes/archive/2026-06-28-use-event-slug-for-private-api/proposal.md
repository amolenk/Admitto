## Why

Private/Partner API routes currently expose internal ticketed event IDs in URLs, even though event websites and attendee-facing links already use the public event slug. Using the public slug for all event-scoped Private API calls gives external event sites a stable, human-readable identifier and avoids requiring partners to store or route with internal event GUIDs.

## What Changes

- **BREAKING**: Replace Private/Partner event-scoped route parameters from `/api/events/{eventId}` to `/api/events/{eventSlug}` for registration, OTP, waitlist, public ticket-type, coupon-details, self-cancel, and self-change endpoints.
- Resolve `{eventSlug}` through `TicketedEvent.PublicSlug` inside the API-key owner team scope before dispatching handlers that still need `TicketedEventId`.
- Preserve the existing `X-Api-Key` authentication model: handlers derive `TeamId` from the API-key principal and never accept team ID or team slug in the route.
- Preserve not-found behavior for unknown slugs and cross-team mismatches: a valid API key for another team must not reveal whether a slug exists elsewhere.
- Update generated OpenAPI contracts and downstream Admin/UI or partner-facing SDK usage affected by the route contract.
- Remove the old `/api/events/{eventId}` Private/Partner routes rather than supporting both identifiers in parallel.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `team-api-keys`: Partner API route identity changes from event ID to public event slug while retaining API-key team scope.
- `attendee-registration`: Self-service registration, coupon registration, and public ticket discovery routes use `{eventSlug}` and resolve to the event ID server-side.
- `ticket-type-management`: Public ticket-type discovery routes use `{eventSlug}` and resolve to the event ID server-side.
- `email-otp-verification`: OTP request and verification routes use `{eventSlug}` and bind issued verification tokens to the resolved event ID and team ID.
- `waitlist`: Waitlist join and leave routes use `{eventSlug}` and keep API-key team scoping.
- `self-service-cancel-registration`: Self-cancel route uses `{eventSlug}` and validates the registration under the resolved event ID.
- `self-service-change-tickets`: Self-service ticket-change route uses `{eventSlug}` and validates the registration under the resolved event ID.
- `coupon-management`: Public coupon details route uses `{eventSlug}` and scopes coupon lookup by resolved event ID plus API-key team.

## Impact

- API routes and endpoint method signatures under `RegistrationsModule` and `*.PartnerApi` slices.
- Partner/private route tests in `Admitto.Api.Tests` and handler/endpoint tests that assert route templates, locations, or not-found behavior.
- Public OpenAPI schema and generated clients that consume `/api/events/{eventId}` paths.
- Architecture documentation that currently describes Partner API routes as `/api/events/{eventId}` in `docs/arc42/06-runtime-view.md` and `docs/arc42/08-crosscutting-concepts.md`.
