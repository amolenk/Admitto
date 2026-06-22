## ADDED Requirements

### Requirement: Public registration endpoints use API-key team scope
The system SHALL expose public attendee registration endpoints under `/api/events/{eventId}` and SHALL require a valid active `X-Api-Key` for each request. Public registration endpoint handlers SHALL derive `TeamId` from the authenticated API-key principal and SHALL NOT accept team ID or team slug in the URL.

Self-service registration SHALL be exposed at `POST /api/events/{eventId}/registrations`. Coupon-based registration SHALL be exposed at `POST /api/events/{eventId}/registrations/coupon`.

#### Scenario: Self-service registration uses API-key team scope
- **WHEN** an attendee posts a valid self-service registration request to `POST /api/events/{eventId}/registrations` with a valid API key for the event's team
- **THEN** the system processes the request using the API key owner's `TeamId` and the route `{eventId}`

#### Scenario: Coupon registration uses API-key team scope
- **WHEN** an attendee posts a valid coupon registration request to `POST /api/events/{eventId}/registrations/coupon` with a valid API key for the event's team
- **THEN** the system processes the request using the API key owner's `TeamId` and the route `{eventId}`

#### Scenario: Registration without API key is rejected
- **WHEN** an attendee posts to either public registration endpoint without `X-Api-Key`
- **THEN** the system returns HTTP 401 and does not run the registration handler
