## MODIFIED Requirements

### Requirement: Public waitlist endpoints use API-key team scope
The system SHALL expose public waitlist endpoints under `/api/events/{eventSlug}` and SHALL require a valid active `X-Api-Key` for each request. Public waitlist endpoint handlers SHALL derive `TeamId` from the authenticated API-key principal, SHALL resolve `{eventSlug}` to the event's internal ID within that team scope, and SHALL NOT accept team ID or team slug in the URL.

Joining a waitlist SHALL be exposed at `POST /api/events/{eventSlug}/waitlist/{ticketTypeId}`. Leaving a waitlist SHALL be exposed at `DELETE /api/events/{eventSlug}/waitlist/{ticketTypeId}`.

#### Scenario: Join waitlist uses API-key team scope
- **WHEN** an attendee posts a valid waitlist join request to `POST /api/events/{eventSlug}/waitlist/{ticketTypeId}` with a valid API key for the event's team
- **THEN** the system processes the request using the API key owner's `TeamId`, the event ID resolved from the route `{eventSlug}`, and the route `{ticketTypeId}`

#### Scenario: Leave waitlist uses API-key team scope
- **WHEN** an attendee sends a valid leave request to `DELETE /api/events/{eventSlug}/waitlist/{ticketTypeId}` with a valid API key for the event's team
- **THEN** the system processes the request using the API key owner's `TeamId`, the event ID resolved from the route `{eventSlug}`, and the route `{ticketTypeId}`

#### Scenario: Waitlist endpoint without API key is rejected
- **WHEN** an attendee calls either public waitlist endpoint without `X-Api-Key`
- **THEN** the system returns HTTP 401 and does not run the waitlist handler
