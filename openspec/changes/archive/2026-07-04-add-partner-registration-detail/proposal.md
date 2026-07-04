## Why

Partner event websites let attendees modify existing registrations through the Partner API, but they currently lack a read endpoint to prefill the edit form. Exposing a scoped Partner API detail endpoint lets trusted partner websites retrieve the attendee-editable registration state before calling the existing update flow.

## What Changes

- Add `GET /api/events/{eventSlug}/registrations/{registrationId}` under the existing Partner API route family.
- Require `X-Api-Key` authentication and resolve `{eventSlug}` within the API key owner's team scope before querying the registration.
- Treat `registrationId` as the attendee-held bearer credential for the registration, consistent with existing partner self-service modification routes.
- Return a reduced registration detail DTO with the fields partner websites need to prefill an edit form: registration identity, email, first name, last name, status, current ticket selection, and additional details.
- Preserve the existing admin registration detail endpoint and payload unchanged.

## Capabilities

### New Capabilities
- `partner-registration-detail`: Partner API retrieval of a scoped, reduced registration detail payload for trusted event websites.

### Modified Capabilities
- None.

## Impact

- Registrations module application slice for a read-only Partner registration detail lookup by resolved team ID, event ID, and registration ID.
- Registrations Partner API endpoint registration under `GET /api/events/{eventSlug}/registrations/{registrationId}`.
- API tests for Partner API authentication, team-scoped event resolution, registration bearer behavior, not-found behavior, and response shape.
- No database schema changes are expected.
