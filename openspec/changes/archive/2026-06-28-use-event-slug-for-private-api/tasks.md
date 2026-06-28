## 1. Route Contract And Resolution

- [x] 1.1 Update the Partner API route group from `/api/events/{eventId:guid}` to `/api/events/{eventSlug}`.
- [x] 1.2 Add or reuse a Registrations module lookup that resolves `TicketedEvent.PublicSlug` to `TicketedEventId` within the API-key owner's `TeamId` scope.
- [x] 1.3 Ensure unknown slugs and slugs outside the API-key owner's team return the existing not-found behavior without leaking cross-team existence.

## 2. Endpoint Updates

- [x] 2.1 Update OTP request and verify endpoints to accept `eventSlug`, resolve the event ID, and continue issuing/validating tokens with `eventId` and `teamId` claims.
- [x] 2.2 Update self-service and coupon registration endpoints to accept `eventSlug`, resolve the event ID, and use the resolved ID in commands and response locations.
- [x] 2.3 Update self-cancel and self-change ticket endpoints to accept `eventSlug`, resolve the event ID, and validate registrations under the resolved event scope.
- [x] 2.4 Update public ticket-type discovery and coupon-details endpoints to accept `eventSlug` and resolve the event ID before querying.
- [x] 2.5 Update waitlist join and leave endpoints to accept `eventSlug` and resolve the event ID before validating tokens or dispatching commands.
- [x] 2.6 Remove or avoid compatibility mappings for the old `/api/events/{eventId}` route shape.

## 3. Generated Contracts And Consumers

- [x] 3.1 Regenerate affected OpenAPI clients from the Aspire-served API spec.
- [x] 3.2 Update all repository call sites, proxy routes, or tests that construct Partner API URLs with event IDs to use public event slugs.

## 4. Tests

- [x] 4.1 Add or update API tests for successful Partner API calls using `/api/events/{eventSlug}`.
- [x] 4.2 Add or update API tests for unknown event slug and cross-team event slug returning not found under a valid API key.
- [x] 4.3 Add or update API tests proving `/api/events/{eventId}` no longer maps to Partner API endpoints.
- [x] 4.4 Run architecture tests first, then targeted API/core tests for the changed Partner API flows.

## 5. Documentation

- [x] 5.1 Update `docs/arc42/06-runtime-view.md` Partner API flow references from `{eventId}` to `{eventSlug}` and describe slug-to-ID resolution.
- [x] 5.2 Update `docs/arc42/08-crosscutting-concepts.md` Partner API authentication and registration-bound link references from `{eventId}` to `{eventSlug}`.
