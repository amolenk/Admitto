## Why

Partner event websites can already let attendees change ticket selections, but the edit experience also needs to update the attendee-facing registration details shown on the same form: first name, last name, and event-specific additional details. Keeping these edits in one atomic call mirrors registration creation and avoids partial updates between attendee identity, additional information, and tickets.

## What Changes

- Replace the ticket-only Partner API self-service mutation with a single registration update endpoint.
- **BREAKING**: Remove the `/api/events/{eventSlug}/registrations/{registrationId}/tickets` Partner endpoint contract instead of preserving backward compatibility.
- Add first-name, last-name, ticket selection, additional-details, and optional waitlist-coupon fields to the self-service update request.
- Implement the new endpoint in a new/different Registrations use-case slice rather than broadening the existing `ChangeAttendeeTickets` slice.
- Validate additional details against the event's current additional-detail schema using the same rules as registration creation.
- Persist attendee detail and ticket changes atomically in one Registrations unit of work.
- Keep Partner API scoping semantics: derive `TeamId` from `X-Api-Key`, resolve `{eventSlug}` within that team, and use `{registrationId}` as the attendee registration bearer credential.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `self-service-change-tickets`: broaden the ticket-only self-service mutation into a full self-service registration update and replace the `/tickets` route contract.

## Impact

- Registrations Partner API route and generated OpenAPI contract change.
- Registrations self-service update command, handler, request DTO, validator, aggregate operation, and tests change.
- Admin UI SDK regeneration may be needed if any UI/proxy code consumes the Partner API contract, though no Admin UI flow is expected to call this endpoint directly.
- Existing external callers of `/registrations/{registrationId}/tickets` must move to the new registration update endpoint.
