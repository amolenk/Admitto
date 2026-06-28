## MODIFIED Requirements

### Requirement: Public endpoint lists self-service-enabled ticket types
The system SHALL expose an API-key-protected public endpoint `GET /api/events/{eventSlug}/ticket-types` that returns all active (not cancelled), self-service-enabled ticket types for the event resolved from `{eventSlug}` within the API-key owner's team scope. The endpoint SHALL derive `TeamId` from the authenticated API-key principal and SHALL use `{eventSlug}` from the URL path. This endpoint is intended for external websites to determine which ticket types to present to attendees.
Cancelled ticket types and ticket types with `SelfServiceEnabled = false` SHALL be excluded from the response. Each ticket type in the response SHALL include:
`id`, name, time slots, max capacity (null if unlimited), and used capacity.

#### Scenario: Returns only self-service-enabled, active ticket types
- **GIVEN** an event has "General Admission" (selfServiceEnabled: true, active), "VIP Pass" (selfServiceEnabled: false, active), and "Early Bird" (selfServiceEnabled: true, cancelled)
- **WHEN** an external caller fetches `GET /api/events/{eventSlug}/ticket-types` with a valid API key for the event's team
- **THEN** only "General Admission" is returned (VIP Pass is admin-only, Early Bird is cancelled)

#### Scenario: Returns empty list when no self-service ticket types exist
- **GIVEN** an event has only admin-only ticket types
- **WHEN** an external caller fetches `GET /api/events/{eventSlug}/ticket-types` with a valid API key for the event's team
- **THEN** an empty list is returned

#### Scenario: Returns 404 when event does not exist
- **WHEN** an external caller fetches ticket types for a non-existent event slug or an event outside the API key owner's team
- **THEN** the response is HTTP 404 Not Found

#### Scenario: Missing API key is rejected
- **WHEN** an external caller fetches ticket types without `X-Api-Key`
- **THEN** the system returns HTTP 401 and does not run the ticket-type listing handler
